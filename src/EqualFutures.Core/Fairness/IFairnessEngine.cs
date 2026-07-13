using EqualFutures.Core.Education;
using EqualFutures.Domain;

namespace EqualFutures.Core.Fairness;

/// <summary>
/// Compares the financial support each child receives across every funding source,
/// under a selectable fairness metric.
/// </summary>
public interface IFairnessEngine
{
    /// <summary>Evaluate fairness for the plan's preferred metric.</summary>
    FairnessResult Evaluate(FinancialPlan plan, IReadOnlyList<EducationProjection> projections);

    /// <summary>Evaluate fairness under a specific metric.</summary>
    FairnessResult Evaluate(FinancialPlan plan, IReadOnlyList<EducationProjection> projections, FairnessMetric metric);
}
