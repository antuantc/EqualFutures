using EqualFutures.Core.Education;
using EqualFutures.Core.Fairness;
using EqualFutures.Core.Retirement;
using EqualFutures.Domain;

namespace EqualFutures.Core.Recommendations;

/// <summary>Generates actionable, explained recommendations from a plan analysis.</summary>
public interface IRecommendationEngine
{
    IReadOnlyList<Recommendation> Generate(
        FinancialPlan plan,
        RetirementProjection retirement,
        IReadOnlyList<EducationProjection> education,
        FairnessResult fairness);
}
