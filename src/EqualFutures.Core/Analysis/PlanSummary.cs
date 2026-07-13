using EqualFutures.Core.Education;
using EqualFutures.Core.Fairness;
using EqualFutures.Core.Recommendations;
using EqualFutures.Core.Retirement;
using EqualFutures.Domain;

namespace EqualFutures.Core.Analysis;

/// <summary>One asset class slice of the household's investment allocation.</summary>
public record AllocationSlice(AccountCategory Category, decimal Amount, decimal Percent);

/// <summary>
/// A complete, pre-computed snapshot of the household's plan used to render the
/// dashboard and every module without recalculating in the UI.
/// </summary>
public record PlanSummary
{
    public decimal TotalAssets { get; init; }
    public decimal TotalLiabilities { get; init; }
    public decimal NetWorth { get; init; }

    public decimal AnnualIncome { get; init; }
    public decimal AnnualSavings { get; init; }
    public decimal AnnualDebtPayments { get; init; }

    public IReadOnlyList<AllocationSlice> Allocation { get; init; } = Array.Empty<AllocationSlice>();

    public RetirementProjection Retirement { get; init; } = new();
    public IReadOnlyList<EducationProjection> Education { get; init; } = Array.Empty<EducationProjection>();
    public FairnessResult Fairness { get; init; } = new();
    public IReadOnlyList<Recommendation> Recommendations { get; init; } = Array.Empty<Recommendation>();

    /// <summary>Percent of every child's family funding target that is projected to be met.</summary>
    public decimal EducationFundedPercent { get; init; }
}
