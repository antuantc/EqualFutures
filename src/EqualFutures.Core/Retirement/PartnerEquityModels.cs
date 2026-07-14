namespace EqualFutures.Core.Retirement;

/// <summary>Retirement-asset position for a single parent, used for equity comparisons.</summary>
public record PartnerEquityLine
{
    public int ParentId { get; init; }
    public string ParentName { get; init; } = string.Empty;
    public int CurrentAge { get; init; }
    public int PlannedRetirementAge { get; init; }

    /// <summary>Investment balance owned by this parent plus their share of joint (unassigned) accounts.</summary>
    public decimal CurrentBalance { get; init; }

    /// <summary>Annual contribution attributed to this parent (owned + joint share), before any fairness top-up.</summary>
    public decimal AnnualContribution { get; init; }

    /// <summary>Projected balance at this parent's own planned retirement age, compounding at the plan's pre-retirement return.</summary>
    public decimal ProjectedBalance { get; init; }

    /// <summary>Share of the combined household projected balance, 0-100.</summary>
    public decimal SharePercent { get; init; }
}

/// <summary>Household retirement-equity comparison across parents.</summary>
public record PartnerEquitySnapshot
{
    public IReadOnlyList<PartnerEquityLine> Parents { get; init; } = Array.Empty<PartnerEquityLine>();

    /// <summary>True only when there are exactly two parents to compare.</summary>
    public bool HasComparablePair { get; init; }

    public int? LeadingParentId { get; init; }
    public int? LaggingParentId { get; init; }

    /// <summary>Projected-balance gap between the leading and lagging parent (always &gt;= 0).</summary>
    public decimal ProjectedGap { get; init; }

    public decimal CombinedProjectedBalance { get; init; }
}

/// <summary>A single year in a fairness catch-up projection.</summary>
public record CatchUpYearPoint(int CalendarYear, decimal LeadingBalance, decimal LaggingBalance)
{
    public decimal Gap => Math.Max(0m, LeadingBalance - LaggingBalance);
}

/// <summary>Result of projecting a monthly fairness top-up (and optional career break) forward for the lagging parent.</summary>
public record CatchUpProjection
{
    public int LeadingParentId { get; init; }
    public int LaggingParentId { get; init; }
    public decimal MonthlyTopUpAmount { get; init; }
    public int CareerBreakYears { get; init; }
    public IReadOnlyList<CatchUpYearPoint> Trajectory { get; init; } = Array.Empty<CatchUpYearPoint>();

    /// <summary>First calendar year the lagging parent's balance reaches the leading parent's, or null if it never does within the horizon.</summary>
    public int? GapClosesYear { get; init; }

    /// <summary>Remaining gap at the end of the projected horizon.</summary>
    public decimal FinalGap { get; init; }
}
