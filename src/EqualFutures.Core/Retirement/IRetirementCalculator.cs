using EqualFutures.Domain;

namespace EqualFutures.Core.Retirement;

/// <summary>Produces retirement projections for a household plan.</summary>
public interface IRetirementCalculator
{
    /// <summary>
    /// Projects retirement readiness for the plan, using the earliest parent
    /// retirement date as the household retirement point.
    /// </summary>
    RetirementProjection Project(FinancialPlan plan);
}
