using EqualFutures.Domain;

namespace EqualFutures.Core.RealEstate;

/// <summary>
/// Projects real estate accounts forward: appreciation for every property, and net rental
/// cash flow for properties marked as a rental.
/// </summary>
public interface IRealEstateCalculator
{
    /// <summary>Projects every real estate account in the plan forward by <paramref name="years"/>.</summary>
    RealEstateSummary Project(FinancialPlan plan, int years);
}
