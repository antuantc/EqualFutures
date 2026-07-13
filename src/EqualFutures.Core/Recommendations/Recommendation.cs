namespace EqualFutures.Core.Recommendations;

public enum RecommendationSeverity
{
    Info,
    Suggestion,
    Warning
}

public enum RecommendationCategory
{
    Retirement,
    Education,
    Fairness,
    Debt,
    Tax,
    Allocation
}

/// <summary>A single actionable insight, always paired with the reasoning behind it.</summary>
public record Recommendation
{
    public required string Title { get; init; }
    public required string Detail { get; init; }

    /// <summary>Plain-language explanation of why this is being suggested.</summary>
    public required string Reasoning { get; init; }

    public RecommendationCategory Category { get; init; }
    public RecommendationSeverity Severity { get; init; }
}
