namespace EqualFutures.Core.Retirement;

/// <summary>Outcome of a retirement projection for the household.</summary>
public record RetirementProjection
{
    /// <summary>Years until the earliest-retiring parent stops working.</summary>
    public int YearsToRetirement { get; init; }

    /// <summary>Age of the earliest-retiring parent when the household starts drawing on the portfolio.</summary>
    public int RetirementAge { get; init; }

    /// <summary>Projected total retirement assets at the retirement date (future dollars).</summary>
    public decimal ProjectedNestEgg { get; init; }

    /// <summary>Nest egg required to fund the spending gap for the full horizon (future dollars).</summary>
    public decimal RequiredNestEgg { get; init; }

    /// <summary>Positive means a surplus, negative means a shortfall (future dollars).</summary>
    public decimal FundingGap { get; init; }

    /// <summary>Sustainable first-year withdrawal from the projected nest egg (future dollars).</summary>
    public decimal SafeAnnualWithdrawal { get; init; }

    /// <summary>Annual retirement spending need in future dollars at the retirement date.</summary>
    public decimal AnnualSpendingNeed { get; init; }

    /// <summary>Guaranteed income (Social Security + pensions + net rental income) in future dollars.</summary>
    public decimal AnnualGuaranteedIncome { get; init; }

    /// <summary>
    /// The portion of <see cref="AnnualGuaranteedIncome"/> that comes from rental real estate
    /// (net of vacancy, operating expenses, capital-expenditure reserve, and debt service).
    /// </summary>
    public decimal AnnualRealEstateIncome { get; init; }

    /// <summary>
    /// Projected real estate equity (value minus any linked mortgage) across every real
    /// estate account at the retirement date (future dollars). Informational only — not
    /// counted toward <see cref="ProjectedNestEgg"/> or <see cref="ReadinessScore"/> since
    /// real estate equity isn't easily liquid.
    /// </summary>
    public decimal ProjectedRealEstateEquity { get; init; }

    /// <summary>The portion of spending that must come from the portfolio (future dollars).</summary>
    public decimal AnnualPortfolioNeed { get; init; }

    /// <summary>0-100 readiness score: projected assets relative to what is required.</summary>
    public decimal ReadinessScore { get; init; }

    public bool OnTrack => FundingGap >= 0;

    /// <summary>
    /// Year-by-year projected retirement asset balance, from today through the
    /// planning horizon age (accumulation while working, then drawdown in retirement).
    /// </summary>
    public IReadOnlyList<BalancePoint> GrowthTrajectory { get; init; } = Array.Empty<BalancePoint>();
}

/// <summary>A single point on a projected balance timeline.</summary>
public record BalancePoint(int Year, int Age, decimal Balance);
