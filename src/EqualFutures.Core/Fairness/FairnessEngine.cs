using EqualFutures.Core.Education;
using EqualFutures.Core.Financials;
using EqualFutures.Domain;

namespace EqualFutures.Core.Fairness;

/// <summary>
/// Default fairness engine. Reduces each child to a single comparable "value" under
/// the chosen metric, then measures how far the household is from treating every
/// child identically. Deliberately unequal choices remain visible via the per-child
/// deviations rather than being hidden.
/// </summary>
public class FairnessEngine : IFairnessEngine
{
    public FairnessResult Evaluate(FinancialPlan plan, IReadOnlyList<EducationProjection> projections)
        => Evaluate(plan, projections, plan.PreferredFairnessMetric);

    public FairnessResult Evaluate(FinancialPlan plan, IReadOnlyList<EducationProjection> projections, FairnessMetric metric)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(projections);

        bool isRatio = metric == FairnessMetric.EqualPercentOfTuition;

        var raw = projections.Select(p => new
        {
            p.ChildId,
            p.ChildName,
            Value = ValueFor(plan, p, metric)
        }).ToList();

        decimal total = raw.Sum(r => r.Value);
        decimal count = raw.Count;
        decimal mean = count > 0 ? total / count : 0m;

        var lines = raw.Select(r => new ChildFairnessLine
        {
            ChildId = r.ChildId,
            ChildName = r.ChildName,
            Value = decimal.Round(r.Value, isRatio ? 4 : 0),
            IsRatio = isRatio,
            Share = total > 0m ? decimal.Round(r.Value / total, 4) : 0m,
            DeviationFromEqual = decimal.Round(r.Value - mean, isRatio ? 4 : 0)
        }).ToList();

        decimal fairnessScore = ComputeScore(raw.Select(r => r.Value).ToList(), mean);

        return new FairnessResult
        {
            Metric = metric,
            MetricDescription = Describe(metric),
            Children = lines,
            EqualTarget = decimal.Round(mean, isRatio ? 4 : 0),
            FairnessScore = decimal.Round(fairnessScore, 1)
        };
    }

    private static decimal ValueFor(FinancialPlan plan, EducationProjection p, FairnessMetric metric)
    {
        decimal inflation = plan.Assumptions.InflationRate;
        decimal presentValueOfContribution =
            FinancialMath.PresentValue(p.DesiredFamilyContribution, inflation, p.YearsUntilCollege);
        decimal assignedAccountValue = AssignedAccountValue(plan, p.ChildId);
        decimal assignedAfterTaxAccountValue = AssignedAfterTaxAccountValue(plan, p.ChildId);

        return metric switch
        {
            FairnessMetric.EqualDollarAmount => p.DesiredFamilyContribution + assignedAccountValue,

            FairnessMetric.EqualInflationAdjustedValue => presentValueOfContribution + assignedAccountValue,

            FairnessMetric.EqualPercentOfTuition => p.TotalCostFutureDollars > 0m
                ? (p.DesiredFamilyContribution + assignedAccountValue) / p.TotalCostFutureDollars
                : 0m,

            // Cumulative real value of everything gifted toward the child.
            FairnessMetric.EqualLifetimeGifts => presentValueOfContribution + assignedAccountValue,

            // Real value adjusted for the tax efficiency of the funding accounts.
            FairnessMetric.EqualAfterTaxBenefit =>
                (presentValueOfContribution * TaxEfficiencyFactor(plan, p.ChildId)) + assignedAfterTaxAccountValue,

            _ => p.DesiredFamilyContribution
        };
    }

    private static decimal AssignedAccountValue(FinancialPlan plan, int childId) =>
        AssignedAccounts(plan, childId).Sum(a => a.CurrentBalance);

    private static decimal AssignedAfterTaxAccountValue(FinancialPlan plan, int childId) =>
        AssignedAccounts(plan, childId).Sum(a => a.CurrentBalance * TaxFactor(a.TaxTreatment));

    private static List<Account> AssignedAccounts(FinancialPlan plan, int childId) =>
        plan.Accounts
            .Where(a => a.BeneficiaryChildId == childId && IsChildFairnessAccount(a))
            .ToList();

    private static bool IsChildFairnessAccount(Account account) =>
        account.Category is AccountCategory.Education or AccountCategory.Investment;

    /// <summary>
    /// Weighted tax efficiency of the education and retirement accounts earmarked
    /// to a child. Tax-free vehicles deliver full value, taxable vehicles less.
    /// Defaults to 1.0 when unknown.
    /// </summary>
    private static decimal TaxEfficiencyFactor(FinancialPlan plan, int childId)
    {
        var accounts = AssignedAccounts(plan, childId);
        if (accounts.Count == 0) return 1.0m;

        decimal weightedTotal = accounts.Sum(a => a.CurrentBalance);
        if (weightedTotal <= 0m) return 1.0m;

        decimal factor = accounts.Sum(a => a.CurrentBalance * TaxFactor(a.TaxTreatment));
        return factor / weightedTotal;
    }

    private static decimal TaxFactor(TaxTreatment treatment) => treatment switch
    {
        TaxTreatment.TaxFree => 1.00m,
        TaxTreatment.TaxDeferred => 0.90m,
        TaxTreatment.Taxable => 0.85m,
        _ => 1.00m
    };

    private static decimal ComputeScore(IReadOnlyList<decimal> values, decimal mean)
    {
        if (values.Count <= 1 || mean <= 0m) return 100m;
        decimal maxDeviation = values.Max(v => Math.Abs(v - mean));
        decimal score = 100m * (1m - maxDeviation / mean);
        return Math.Clamp(score, 0m, 100m);
    }

    private static string Describe(FairnessMetric metric) => metric switch
    {
        FairnessMetric.EqualDollarAmount => "Each child receives the same total family contribution in raw dollars.",
        FairnessMetric.EqualInflationAdjustedValue => "Each child receives the same value once adjusted for inflation to today's dollars.",
        FairnessMetric.EqualPercentOfTuition => "The family covers the same percentage of each child's total cost.",
        FairnessMetric.EqualLifetimeGifts => "Each child receives the same cumulative real value of lifetime gifts.",
        FairnessMetric.EqualAfterTaxBenefit => "Each child receives the same value after accounting for the tax efficiency of the funding accounts.",
        _ => string.Empty
    };
}
