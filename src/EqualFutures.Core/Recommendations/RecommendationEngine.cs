using EqualFutures.Core.Education;
using EqualFutures.Core.Fairness;
using EqualFutures.Core.Financials;
using EqualFutures.Core.Retirement;
using EqualFutures.Domain;

namespace EqualFutures.Core.Recommendations;

/// <summary>
/// Rule-based recommendation engine. Retirement is prioritised first because parents
/// cannot borrow for it; education and fairness follow. Every recommendation carries
/// its reasoning so the household understands the tradeoff.
/// </summary>
public class RecommendationEngine : IRecommendationEngine
{
    public IReadOnlyList<Recommendation> Generate(
        FinancialPlan plan,
        RetirementProjection retirement,
        IReadOnlyList<EducationProjection> education,
        FairnessResult fairness)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var recs = new List<Recommendation>();

        AddRetirement(plan, retirement, recs);
        AddEducation(plan, education, recs);
        AddFairness(fairness, recs);
        AddDebt(plan, recs);
        AddTax(plan, recs);
        AddAllocation(plan, retirement, education, recs);

        return recs;
    }

    private static void AddRetirement(FinancialPlan plan, RetirementProjection r, List<Recommendation> recs)
    {
        if (r.FundingGap < 0m)
        {
            decimal shortfall = Math.Abs(r.FundingGap);
            decimal factor = FinancialMath.FutureValueOfAnnuity(1m, plan.Assumptions.PreRetirementReturn, r.YearsToRetirement);
            decimal extraAnnual = factor > 0m ? shortfall / factor : shortfall;

            // Don't recommend contributions the household can't legally make — cap the
            // tax-advantaged portion at this year's remaining 401(k)/IRA/HSA room and
            // route anything beyond that to a taxable brokerage instead.
            decimal headroom = RemainingContributionRoom(plan);
            string detail;
            string reasoning;

            if (extraAnnual <= headroom)
            {
                detail = $"Save about {extraAnnual / 12m:C0} more per month toward retirement to close a projected {shortfall:C0} gap.";
                reasoning = "You cannot borrow for retirement the way you can for college, so retirement is funded first. " +
                            $"At your current pace the portfolio is projected to fall {shortfall:C0} short of the {r.RequiredNestEgg:C0} needed to sustain spending through age {plan.Assumptions.PlanningHorizonAge}.";
            }
            else
            {
                decimal viaTaxable = extraAnnual - headroom;
                detail = headroom > 0m
                    ? $"Max out the remaining {headroom / 12m:C0}/mo of 401(k)/IRA/HSA room, then add about {viaTaxable / 12m:C0}/mo to a taxable brokerage to close a projected {shortfall:C0} gap."
                    : $"Your 401(k)/IRA/HSA contributions are already at this year's IRS limit — add about {viaTaxable / 12m:C0}/mo to a taxable brokerage instead to close a projected {shortfall:C0} gap.";
                reasoning = "You cannot borrow for retirement the way you can for college, so retirement is funded first. " +
                            $"At your current pace the portfolio is projected to fall {shortfall:C0} short of the {r.RequiredNestEgg:C0} needed to sustain spending through age {plan.Assumptions.PlanningHorizonAge}. " +
                            "The full increase exceeds what fits inside this year's tax-advantaged contribution limits, so the rest needs a taxable account.";
            }

            recs.Add(new Recommendation
            {
                Category = RecommendationCategory.Retirement,
                Severity = RecommendationSeverity.Warning,
                Title = "Increase retirement savings",
                Detail = detail,
                Reasoning = reasoning
            });

            recs.Add(new Recommendation
            {
                Category = RecommendationCategory.Retirement,
                Severity = RecommendationSeverity.Suggestion,
                Title = "Consider delaying retirement",
                Detail = "Working two more years lets savings compound longer and shortens the withdrawal period.",
                Reasoning = "Delaying retirement is often the single most effective lever: it adds contributions, extends compounding, and reduces the number of years the portfolio must cover."
            });
        }
        else if (r.ReadinessScore >= 100m && r.FundingGap > r.RequiredNestEgg * 0.15m)
        {
            recs.Add(new Recommendation
            {
                Category = RecommendationCategory.Retirement,
                Severity = RecommendationSeverity.Info,
                Title = "Retirement is on track — room to help with college",
                Detail = $"You are projected to exceed your retirement target by {r.FundingGap:C0}.",
                Reasoning = "With retirement secure, some surplus savings can be redirected toward education funding without jeopardising your own security."
            });
        }
    }

    /// <summary>
    /// Total remaining annual contribution room across the household's 401(k), IRA, and
    /// HSA accounts under this year's IRS limits (see <see cref="ContributionLimits"/>).
    /// Traditional and Roth IRA share one combined limit per owner; 401(k) and HSA limits
    /// apply per account, since each is its own individual instrument.
    /// </summary>
    private static decimal RemainingContributionRoom(FinancialPlan plan)
    {
        var parentAges = plan.Parents.ToDictionary(p => p.Id, p => p.CurrentAge);
        int? AgeOf(int? ownerParentId) => ownerParentId is int id && parentAges.TryGetValue(id, out var age) ? age : null;

        var investmentAccounts = plan.Accounts.Where(a => a.Category == AccountCategory.Investment).ToList();

        decimal room = investmentAccounts
            .Where(a => a.Type == AccountType.FourZeroOneK)
            .Sum(a => Math.Max(0m, ContributionLimits.FourZeroOneKLimit(AgeOf(a.OwnerParentId)) - a.AnnualContribution));

        room += investmentAccounts
            .Where(a => a.Type == AccountType.Hsa)
            .Sum(a => Math.Max(0m, ContributionLimits.Hsa - a.AnnualContribution));

        room += investmentAccounts
            .Where(a => a.Type is AccountType.TraditionalIra or AccountType.RothIra)
            .GroupBy(a => a.OwnerParentId)
            .Sum(g => Math.Max(0m, ContributionLimits.IraLimit(AgeOf(g.Key)) - g.Sum(a => a.AnnualContribution)));

        return room;
    }

    private static void AddEducation(FinancialPlan plan, IReadOnlyList<EducationProjection> education, List<Recommendation> recs)
    {
        foreach (var e in education.Where(e => e.FundingGap > 0m))
        {
            int months = Math.Max(1, e.YearsUntilCollege * 12);
            decimal factor = FinancialMath.FutureValueOfAnnuity(1m, plan.Assumptions.PreRetirementReturn, e.YearsUntilCollege);
            decimal extraAnnual = factor > 0m ? e.FundingGap / factor : e.FundingGap / Math.Max(1, e.YearsUntilCollege);

            decimal existingAnnual = plan.Accounts
                .Where(a => a.Category == AccountCategory.Education && a.BeneficiaryChildId == e.ChildId)
                .Sum(a => a.AnnualContribution);
            bool exceedsGiftExclusion = existingAnnual + extraAnnual > ContributionLimits.GiftTaxExclusionPerDonor;

            recs.Add(new Recommendation
            {
                Category = RecommendationCategory.Education,
                Severity = RecommendationSeverity.Suggestion,
                Title = $"Increase 529 contributions for {e.ChildName}",
                Detail = $"Add about {extraAnnual / 12m:C0} per month to close a projected {e.FundingGap:C0} funding gap.",
                Reasoning = $"Your funding target for {e.ChildName} is {e.DesiredFamilyContribution:C0}, but earmarked savings are projected to reach only {e.ProjectedEducationSavings:C0} by {e.CollegeStartYear}. A 529 grows tax-free for qualified education expenses." +
                            (exceedsGiftExclusion
                                ? $" Note: total contributions would exceed the {ContributionLimits.GiftTaxExclusionPerDonor:C0}/yr per-donor gift-tax exclusion — a 709 form may be required (a 529 has no IRS contribution cap, so this is a filing formality, not a funding limit)."
                                : "")
            });
        }
    }

    /// <summary>
    /// Federal contribution limits used to keep recommendations legally contributable.
    /// 2025 IRS figures; models the standard age-50+ catch-up but not the SECURE 2.0
    /// "super catch-up" for ages 60-63 or the HSA family-vs-self-only split. Update
    /// these as tax law changes.
    /// </summary>
    private static class ContributionLimits
    {
        private const decimal FourZeroOneK = 23_500m;
        private const decimal FourZeroOneKCatchUp = 7_500m;
        private const decimal Ira = 7_000m;
        private const decimal IraCatchUp = 1_000m;
        private const int CatchUpAge = 50;

        /// <summary>Self-only HDHP coverage; family coverage is higher and isn't modelled.</summary>
        public const decimal Hsa = 4_300m;

        /// <summary>Annual per-donor, per-beneficiary gift-tax exclusion — relevant to large 529 gifts.</summary>
        public const decimal GiftTaxExclusionPerDonor = 19_000m;

        public static decimal FourZeroOneKLimit(int? ownerAge) =>
            ownerAge >= CatchUpAge ? FourZeroOneK + FourZeroOneKCatchUp : FourZeroOneK;

        public static decimal IraLimit(int? ownerAge) =>
            ownerAge >= CatchUpAge ? Ira + IraCatchUp : Ira;

        /// <summary>The flat (non-age-adjusted) annual limit for a given account type, or null if uncapped.</summary>
        public static decimal? AnnualLimit(AccountType type) => type switch
        {
            AccountType.FourZeroOneK => FourZeroOneK,
            AccountType.TraditionalIra or AccountType.RothIra => Ira,
            AccountType.Hsa => Hsa,
            _ => null
        };
    }

    private static void AddFairness(FairnessResult fairness, List<Recommendation> recs)
    {
        if (fairness.Children.Count < 2 || fairness.IsBalanced) return;

        var under = fairness.Children.OrderBy(c => c.Value).First();
        var over = fairness.Children.OrderByDescending(c => c.Value).First();

        recs.Add(new Recommendation
        {
            Category = RecommendationCategory.Fairness,
            Severity = RecommendationSeverity.Suggestion,
            Title = "Support across children is uneven",
            Detail = $"Under \"{fairness.MetricDescription}\", {under.ChildName} currently receives the least and {over.ChildName} the most.",
            Reasoning = $"Your fairness balance score is {fairness.FairnessScore:0}/100. If uneven treatment is intentional this is fine — but if not, shifting funding toward {under.ChildName} would move you closer to your chosen fairness goal."
        });
    }

    private static void AddDebt(FinancialPlan plan, List<Recommendation> recs)
    {
        decimal threshold = plan.Assumptions.PostRetirementReturn;
        foreach (var debt in plan.Liabilities.Where(d => d.InterestRate > threshold && d.CurrentBalance > 0m))
        {
            recs.Add(new Recommendation
            {
                Category = RecommendationCategory.Debt,
                Severity = RecommendationSeverity.Suggestion,
                Title = $"Prioritise paying down {debt.Name}",
                Detail = $"This debt's {debt.InterestRate:P1} rate exceeds your expected investment return.",
                Reasoning = $"Paying down debt at {debt.InterestRate:P1} is a guaranteed return that beats the {threshold:P1} you expect from investing, so extra dollars go further here."
            });
        }
    }

    private static void AddTax(FinancialPlan plan, List<Recommendation> recs)
    {
        decimal deferred = plan.Accounts.Where(a => a.TaxTreatment == TaxTreatment.TaxDeferred).Sum(a => a.CurrentBalance);
        decimal taxFree = plan.Accounts.Where(a => a.TaxTreatment == TaxTreatment.TaxFree).Sum(a => a.CurrentBalance);

        if (deferred > 0m && deferred > taxFree * 3m)
        {
            recs.Add(new Recommendation
            {
                Category = RecommendationCategory.Tax,
                Severity = RecommendationSeverity.Info,
                Title = "Explore Roth conversion opportunities",
                Detail = "A large share of savings is in tax-deferred accounts.",
                Reasoning = $"You hold {deferred:C0} in tax-deferred accounts versus {taxFree:C0} tax-free. Converting some to Roth in lower-income years can reduce future required minimum distributions and lifetime taxes."
            });
        }
    }

    /// <summary>
    /// Account-level "where should the next dollar go" guidance: which accounts deserve
    /// more, which have gone idle, and which have grown past their goal and could be
    /// redirected or moved elsewhere.
    /// </summary>
    private static void AddAllocation(FinancialPlan plan, RetirementProjection retirement, IReadOnlyList<EducationProjection> education, List<Recommendation> recs)
    {
        AddHsaPriority(plan, recs);
        AddTaxAdvantagedOverTaxable(plan, recs);
        AddIdleCash(plan, retirement, education, recs);
        AddOverfundedEducationAccounts(plan, education, recs);
    }

    private static void AddHsaPriority(FinancialPlan plan, List<Recommendation> recs)
    {
        var underfundedHsas = plan.Accounts.Where(a => a.Type == AccountType.Hsa && a.AnnualContribution <= 0m).ToList();
        if (underfundedHsas.Count == 0) return;

        recs.Add(new Recommendation
        {
            Category = RecommendationCategory.Allocation,
            Severity = RecommendationSeverity.Suggestion,
            Title = "Prioritise your HSA",
            Detail = $"{string.Join(", ", underfundedHsas.Select(a => a.Name))} {(underfundedHsas.Count == 1 ? "isn't" : "aren't")} receiving new contributions. This year's limit is {ContributionLimits.Hsa:C0} (self-only coverage).",
            Reasoning = "A health savings account is the only account with a triple tax advantage: contributions are deductible, growth is tax-free, and withdrawals for qualified medical expenses are never taxed. Most advisors rank it above a traditional 401(k) or IRA once any employer match is captured."
        });
    }

    private static void AddTaxAdvantagedOverTaxable(FinancialPlan plan, List<Recommendation> recs)
    {
        var investment = plan.Accounts.Where(a => a.Category == AccountCategory.Investment).ToList();
        var fundedTaxable = investment.Where(a => a.TaxTreatment == TaxTreatment.Taxable && a.AnnualContribution > 0m).ToList();
        var idleAdvantaged = investment.Where(a => a.TaxTreatment != TaxTreatment.Taxable && a.AnnualContribution <= 0m).ToList();

        if (fundedTaxable.Count == 0 || idleAdvantaged.Count == 0) return;

        decimal taxableAnnual = fundedTaxable.Sum(a => a.AnnualContribution);
        decimal idleRoom = idleAdvantaged.Sum(a => ContributionLimits.AnnualLimit(a.Type) ?? 0m);
        decimal redirectable = Math.Min(taxableAnnual, idleRoom);

        string detail = redirectable < taxableAnnual
            ? $"You're contributing {taxableAnnual:C0}/yr to {string.Join(", ", fundedTaxable.Select(a => a.Name))} (taxable) while {string.Join(", ", idleAdvantaged.Select(a => a.Name))} {(idleAdvantaged.Count == 1 ? "gets" : "get")} nothing added. Up to {redirectable:C0}/yr fits inside this year's contribution limits for those accounts."
            : $"You're contributing {taxableAnnual:C0}/yr to {string.Join(", ", fundedTaxable.Select(a => a.Name))} (taxable) while {string.Join(", ", idleAdvantaged.Select(a => a.Name))} {(idleAdvantaged.Count == 1 ? "gets" : "get")} nothing added.";

        recs.Add(new Recommendation
        {
            Category = RecommendationCategory.Allocation,
            Severity = RecommendationSeverity.Suggestion,
            Title = "Fund tax-advantaged accounts before your brokerage",
            Detail = detail,
            Reasoning = "Tax-deferred and tax-free accounts shelter growth from tax every year; a taxable brokerage doesn't. Redirecting new savings to the tax-advantaged accounts first, up to this year's IRS limits, then using the brokerage for anything beyond that, generally leaves more money compounding for the same contribution."
        });
    }

    private static void AddIdleCash(FinancialPlan plan, RetirementProjection retirement, IReadOnlyList<EducationProjection> education, List<Recommendation> recs)
    {
        var bankAccounts = plan.Accounts.Where(a => a.Type == AccountType.BankAccount).ToList();
        decimal cash = bankAccounts.Sum(a => a.CurrentBalance);
        if (cash <= 0m || plan.ExpectedAnnualRetirementSpending <= 0m) return;

        decimal emergencyFundTarget = plan.ExpectedAnnualRetirementSpending * 0.5m; // ~6 months of spending
        decimal excess = cash - emergencyFundTarget;
        if (excess <= 0m) return;

        string destination;
        if (retirement.FundingGap < 0m)
        {
            destination = "your retirement accounts, which are currently projected to fall short";
        }
        else
        {
            var neediest = education.Where(e => e.FundingGap > 0m).OrderByDescending(e => e.FundingGap).FirstOrDefault();
            destination = neediest is not null
                ? $"{neediest.ChildName}'s 529, which has a {neediest.FundingGap:C0} projected funding gap"
                : "your investment accounts so it isn't losing purchasing power to inflation";
        }

        recs.Add(new Recommendation
        {
            Category = RecommendationCategory.Allocation,
            Severity = RecommendationSeverity.Suggestion,
            Title = "Put idle cash to work",
            Detail = $"{string.Join(", ", bankAccounts.Select(a => a.Name))} holds {cash:C0}, about {excess:C0} more than a 6-month emergency fund needs.",
            Reasoning = $"Cash sitting beyond your emergency reserve loses purchasing power to inflation with no offsetting growth. Consider moving the excess into {destination}."
        });
    }

    private static void AddOverfundedEducationAccounts(FinancialPlan plan, IReadOnlyList<EducationProjection> education, List<Recommendation> recs)
    {
        foreach (var e in education.Where(e =>
            e.ProjectedEducationSavings > 0m &&
            e.DesiredFamilyContribution > 0m &&
            e.ProjectedEducationSavings > e.DesiredFamilyContribution * 1.25m))
        {
            var accounts = plan.Accounts.Where(a => a.Category == AccountCategory.Education && a.BeneficiaryChildId == e.ChildId).ToList();
            if (accounts.Count == 0) continue;

            decimal surplus = e.ProjectedEducationSavings - e.DesiredFamilyContribution;

            recs.Add(new Recommendation
            {
                Category = RecommendationCategory.Allocation,
                Severity = RecommendationSeverity.Info,
                Title = $"{e.ChildName}'s education savings are ahead of target",
                Detail = $"{string.Join(", ", accounts.Select(a => a.Name))} {(accounts.Count == 1 ? "is" : "are")} projected to reach {e.ProjectedEducationSavings:C0}, about {surplus:C0} more than the {e.DesiredFamilyContribution:C0} funding target.",
                Reasoning = "Consider redirecting new contributions elsewhere — toward retirement, a sibling's account, or another goal. If the funds truly won't be needed for education, a 529 can also change beneficiaries to another child, or, subject to IRS rules on account age and lifetime limits, roll leftover funds into the beneficiary's Roth IRA under SECURE 2.0."
            });
        }
    }
}
