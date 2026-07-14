using EqualFutures.Core.Financials;
using EqualFutures.Core.RealEstate;
using EqualFutures.Domain;

namespace EqualFutures.Core.Retirement;

/// <summary>
/// Deterministic retirement projection. Retirement assets grow with contributions
/// until the earliest parent's planned retirement, then must cover the portion of
/// household spending not met by Social Security, pensions, and net rental income.
/// </summary>
public class RetirementCalculator(IRealEstateCalculator realEstateCalculator) : IRetirementCalculator
{
    /// <summary>Account categories that count toward the retirement nest egg.</summary>
    private static bool IsRetirementAsset(Account a) =>
        a.Category == AccountCategory.Investment;

    public RetirementProjection Project(FinancialPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.Parents.Count == 0)
            return new RetirementProjection();

        var assumptions = plan.Assumptions;

        // The household begins drawing on the portfolio when the first parent retires.
        var primary = plan.Parents
            .OrderBy(p => p.PlannedRetirementAge - p.CurrentAge)
            .First();
        int years = Math.Max(0, primary.PlannedRetirementAge - primary.CurrentAge);
        int retirementAge = primary.CurrentAge + years;

        // Grow each retirement account to the retirement date.
        var retirementAccounts = plan.Accounts.Where(IsRetirementAsset).ToList();
        decimal projectedNestEgg = retirementAccounts.Sum(a =>
            FinancialMath.ProjectBalance(
                a.CurrentBalance,
                a.AnnualContribution,
                a.ExpectedReturnOverride ?? assumptions.PreRetirementReturn,
                years));

        // Household spending need in future dollars.
        decimal todaysSpending = plan.ExpectedAnnualRetirementSpending;
        decimal annualSpendingNeed = FinancialMath.InflateValue(todaysSpending, assumptions.InflationRate, years);

        // Guaranteed income assumed to be inflation-indexed to the retirement date.
        // Social Security cannot start before age 62 (or a parent's own later claiming
        // age) — if a parent retires before then, their benefit isn't counted yet.
        decimal annualParentIncome = plan.Parents.Sum(p =>
        {
            int ageAtRetirement = p.CurrentAge + years;
            int earliestClaimingAge = Math.Max(62, p.SocialSecurityClaimingAge);
            decimal socialSecurity = ageAtRetirement >= earliestClaimingAge ? p.EstimatedAnnualSocialSecurity : 0m;
            return FinancialMath.InflateValue(socialSecurity + p.AnnualPensionIncome, assumptions.InflationRate, years);
        });

        // Real estate: every property appreciates; rentals also contribute net cash flow
        // (after vacancy, expenses, and debt service) to guaranteed income. Equity itself
        // is informational only — it isn't liquid the way portfolio assets are.
        var realEstate = realEstateCalculator.Project(plan, years);
        decimal annualGuaranteedIncome = annualParentIncome + realEstate.TotalAnnualRentalIncome;

        decimal annualPortfolioNeed = Math.Max(0m, annualSpendingNeed - annualGuaranteedIncome);

        // Nest egg required to sustain the (inflation-adjusted) portfolio need from
        // retirement through the planning horizon, discounted at the real post-retirement
        // return. This is what makes PostRetirementReturn and PlanningHorizonAge actually
        // drive the required nest egg, rather than the flat safe-withdrawal-rate shortcut.
        int retirementDurationYears = Math.Max(0, assumptions.PlanningHorizonAge - retirementAge);
        decimal realPostRetirementRate = FinancialMath.RealRate(assumptions.PostRetirementReturn, assumptions.InflationRate);
        decimal requiredNestEgg = FinancialMath.PresentValueOfAnnuityDue(annualPortfolioNeed, realPostRetirementRate, retirementDurationYears);

        decimal safeWithdrawal = projectedNestEgg * assumptions.SafeWithdrawalRate;
        decimal fundingGap = projectedNestEgg - requiredNestEgg;

        decimal readiness = requiredNestEgg > 0m
            ? Math.Clamp(projectedNestEgg / requiredNestEgg * 100m, 0m, 100m)
            : 100m;

        return new RetirementProjection
        {
            YearsToRetirement = years,
            RetirementAge = retirementAge,
            ProjectedNestEgg = projectedNestEgg,
            RequiredNestEgg = requiredNestEgg,
            FundingGap = fundingGap,
            SafeAnnualWithdrawal = safeWithdrawal,
            AnnualSpendingNeed = annualSpendingNeed,
            AnnualGuaranteedIncome = annualGuaranteedIncome,
            AnnualRealEstateIncome = realEstate.TotalAnnualRentalIncome,
            ProjectedRealEstateEquity = realEstate.TotalProjectedEquity,
            AnnualPortfolioNeed = annualPortfolioNeed,
            ReadinessScore = decimal.Round(readiness, 1),
            GrowthTrajectory = BuildTrajectory(retirementAccounts, assumptions, primary.CurrentAge, years, retirementDurationYears, annualPortfolioNeed)
        };
    }

    private static IReadOnlyList<BalancePoint> BuildTrajectory(
        List<Account> accounts, PlanAssumptions assumptions, int startAge, int yearsToRetirement, int retirementDurationYears, decimal annualPortfolioNeedAtRetirement)
    {
        var points = new List<BalancePoint>(yearsToRetirement + retirementDurationYears + 1);
        int currentYear = DateTime.UtcNow.Year;

        // Accumulation phase: accounts grow with contributions until retirement.
        decimal balanceAtRetirement = 0m;
        for (int y = 0; y <= yearsToRetirement; y++)
        {
            decimal balance = accounts.Sum(a =>
                FinancialMath.ProjectBalance(
                    a.CurrentBalance,
                    a.AnnualContribution,
                    a.ExpectedReturnOverride ?? assumptions.PreRetirementReturn,
                    y));
            points.Add(new BalancePoint(currentYear + y, startAge + y, decimal.Round(balance, 0)));
            balanceAtRetirement = balance;
        }

        // Drawdown phase: the portfolio grows at the post-retirement return and is drawn
        // down each year by the portfolio need, which keeps pace with inflation.
        decimal drawdownBalance = balanceAtRetirement;
        for (int y = 1; y <= retirementDurationYears; y++)
        {
            decimal withdrawal = FinancialMath.InflateValue(annualPortfolioNeedAtRetirement, assumptions.InflationRate, y);
            drawdownBalance = Math.Max(0m, drawdownBalance * (1m + assumptions.PostRetirementReturn) - withdrawal);
            points.Add(new BalancePoint(currentYear + yearsToRetirement + y, startAge + yearsToRetirement + y, decimal.Round(drawdownBalance, 0)));
        }

        return points;
    }
}
