using EqualFutures.Domain;

namespace EqualFutures.Core.Education;

/// <summary>Education funding outcome for a single child.</summary>
public record EducationProjection
{
    public int ChildId { get; init; }
    public string ChildName { get; init; } = string.Empty;
    public int CollegeStartYear { get; init; }
    public int YearsUntilCollege { get; init; }

    /// <summary>Total cost across all college years in today's dollars.</summary>
    public decimal TotalCostTodaysDollars { get; init; }

    /// <summary>Total cost across all college years inflated to when they are incurred.</summary>
    public decimal TotalCostFutureDollars { get; init; }

    /// <summary>Projected value of earmarked education accounts by the start of college.</summary>
    public decimal ProjectedEducationSavings { get; init; }

    /// <summary>Total expected scholarships / grants (future dollars).</summary>
    public decimal ExpectedScholarships { get; init; }

    /// <summary>Portion of cost the family intends to cover (future dollars).</summary>
    public decimal DesiredFamilyContribution { get; init; }

    /// <summary>Cost expected to fall to the student (loans / work) after family funding target.</summary>
    public decimal StudentResponsibility { get; init; }

    /// <summary>
    /// Unfunded gap: family funding target not yet covered by projected savings
    /// (future dollars). Positive means more saving is needed.
    /// </summary>
    public decimal FundingGap { get; init; }

    /// <summary>Percent of the family funding target already covered by projected savings.</summary>
    public decimal PercentFunded { get; init; }
}
