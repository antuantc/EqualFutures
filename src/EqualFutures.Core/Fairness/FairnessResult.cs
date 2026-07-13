using EqualFutures.Domain;

namespace EqualFutures.Core.Fairness;

/// <summary>How one child compares to their siblings under a chosen fairness metric.</summary>
public record ChildFairnessLine
{
    public int ChildId { get; init; }
    public string ChildName { get; init; } = string.Empty;

    /// <summary>The child's measured value under the selected metric (dollars, or a ratio 0-1).</summary>
    public decimal Value { get; init; }

    /// <summary>Whether <see cref="Value"/> is a ratio (percent metrics) rather than a dollar amount.</summary>
    public bool IsRatio { get; init; }

    /// <summary>The child's share of the total across all children (0-1).</summary>
    public decimal Share { get; init; }

    /// <summary>
    /// Difference between this child's value and the equal-treatment target.
    /// Positive means the child receives more than an equal share.
    /// </summary>
    public decimal DeviationFromEqual { get; init; }
}

/// <summary>Result of evaluating fairness across all children for one metric.</summary>
public record FairnessResult
{
    public FairnessMetric Metric { get; init; }
    public string MetricDescription { get; init; } = string.Empty;
    public IReadOnlyList<ChildFairnessLine> Children { get; init; } = Array.Empty<ChildFairnessLine>();

    /// <summary>The equal-treatment target every child would receive if perfectly balanced.</summary>
    public decimal EqualTarget { get; init; }

    /// <summary>0-100 balance score. 100 means every child is treated identically.</summary>
    public decimal FairnessScore { get; init; }

    /// <summary>True when the spread between children is negligible.</summary>
    public bool IsBalanced => FairnessScore >= 95m;
}
