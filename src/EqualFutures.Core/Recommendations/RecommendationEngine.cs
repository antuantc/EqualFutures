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

        return recs;
    }

    private static void AddRetirement(FinancialPlan plan, RetirementProjection r, List<Recommendation> recs)
    {
        if (r.FundingGap < 0m)
        {
            decimal shortfall = Math.Abs(r.FundingGap);
            decimal factor = FinancialMath.FutureValueOfAnnuity(1m, plan.Assumptions.PreRetirementReturn, r.YearsToRetirement);
            decimal extraAnnual = factor > 0m ? shortfall / factor : shortfall;

            recs.Add(new Recommendation
            {
                Category = RecommendationCategory.Retirement,
                Severity = RecommendationSeverity.Warning,
                Title = "Increase retirement savings",
                Detail = $"Save about {extraAnnual / 12m:C0} more per month toward retirement to close a projected {shortfall:C0} gap.",
                Reasoning = "You cannot borrow for retirement the way you can for college, so retirement is funded first. " +
                            $"At your current pace the portfolio is projected to fall {shortfall:C0} short of the {r.RequiredNestEgg:C0} needed to sustain spending through age {plan.Assumptions.PlanningHorizonAge}."
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

    private static void AddEducation(FinancialPlan plan, IReadOnlyList<EducationProjection> education, List<Recommendation> recs)
    {
        foreach (var e in education.Where(e => e.FundingGap > 0m))
        {
            int months = Math.Max(1, e.YearsUntilCollege * 12);
            decimal factor = FinancialMath.FutureValueOfAnnuity(1m, plan.Assumptions.PreRetirementReturn, e.YearsUntilCollege);
            decimal extraAnnual = factor > 0m ? e.FundingGap / factor : e.FundingGap / Math.Max(1, e.YearsUntilCollege);

            recs.Add(new Recommendation
            {
                Category = RecommendationCategory.Education,
                Severity = RecommendationSeverity.Suggestion,
                Title = $"Increase 529 contributions for {e.ChildName}",
                Detail = $"Add about {extraAnnual / 12m:C0} per month to close a projected {e.FundingGap:C0} funding gap.",
                Reasoning = $"Your funding target for {e.ChildName} is {e.DesiredFamilyContribution:C0}, but earmarked savings are projected to reach only {e.ProjectedEducationSavings:C0} by {e.CollegeStartYear}. A 529 grows tax-free for qualified education expenses."
            });
        }
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
}
