using EqualFutures.Core.Education;
using EqualFutures.Core.Fairness;
using EqualFutures.Core.Recommendations;
using EqualFutures.Core.Retirement;
using EqualFutures.Domain;

namespace EqualFutures.Core.Analysis;

/// <summary>
/// Orchestrates the calculation modules into a single dashboard-ready snapshot.
/// The UI depends only on this service, keeping presentation free of financial logic.
/// </summary>
public class PlanAnalysisService(
    IRetirementCalculator retirement,
    IEducationCalculator education,
    IFairnessEngine fairness,
    IRecommendationEngine recommendations) : IPlanAnalysisService
{
    public PlanSummary Analyze(FinancialPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var retirementProjection = retirement.Project(plan);
        var educationProjections = education.ProjectAll(plan);
        var fairnessResult = fairness.Evaluate(plan, educationProjections);
        var recs = recommendations.Generate(plan, retirementProjection, educationProjections, fairnessResult);

        decimal totalAssets = plan.Accounts.Sum(a => a.CurrentBalance);
        decimal totalLiabilities = plan.Liabilities.Sum(l => l.CurrentBalance);

        var allocation = plan.Accounts
            .GroupBy(a => a.Category)
            .Select(g =>
            {
                decimal amount = g.Sum(a => a.CurrentBalance);
                decimal pct = totalAssets > 0m ? decimal.Round(amount / totalAssets * 100m, 1) : 0m;
                return new AllocationSlice(g.Key, amount, pct);
            })
            .OrderByDescending(s => s.Amount)
            .ToList();

        decimal desiredTotal = educationProjections.Sum(e => e.DesiredFamilyContribution);
        decimal savedTotal = educationProjections.Sum(e => Math.Min(e.ProjectedEducationSavings, e.DesiredFamilyContribution));
        decimal educationFundedPercent = desiredTotal > 0m
            ? decimal.Round(savedTotal / desiredTotal * 100m, 1)
            : 100m;

        return new PlanSummary
        {
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            NetWorth = totalAssets - totalLiabilities,
            AnnualIncome = plan.Parents.Sum(p => p.AnnualIncome),
            AnnualSavings = plan.Accounts.Sum(a => a.AnnualContribution),
            AnnualDebtPayments = plan.Liabilities.Sum(l => l.MonthlyPayment * 12m),
            Allocation = allocation,
            Retirement = retirementProjection,
            Education = educationProjections,
            Fairness = fairnessResult,
            Recommendations = recs,
            EducationFundedPercent = educationFundedPercent
        };
    }
}
