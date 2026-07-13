using EqualFutures.Domain;

namespace EqualFutures.Core.Education;

/// <summary>Estimates college cost and funding gaps per child.</summary>
public interface IEducationCalculator
{
    /// <summary>Default annual cost (today's dollars) for a college path.</summary>
    decimal DefaultAnnualCost(CollegeType type);

    /// <summary>Projects education funding for a single child.</summary>
    EducationProjection Project(FinancialPlan plan, Child child);

    /// <summary>Projects education funding for every child in the plan.</summary>
    IReadOnlyList<EducationProjection> ProjectAll(FinancialPlan plan);
}
