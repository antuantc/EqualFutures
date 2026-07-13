using EqualFutures.Domain;

namespace EqualFutures.Core.Analysis;

/// <summary>Builds a full <see cref="PlanSummary"/> by running every calculation module.</summary>
public interface IPlanAnalysisService
{
    PlanSummary Analyze(FinancialPlan plan);
}
