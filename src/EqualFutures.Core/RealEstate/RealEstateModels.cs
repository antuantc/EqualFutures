namespace EqualFutures.Core.RealEstate;

/// <summary>Projected value, equity, and (for rentals) net cash flow for a single real estate account.</summary>
public record RealEstateProjection
{
    public int AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public EqualFutures.Domain.RealEstateUse Use { get; init; }

    /// <summary>Projected gross property value (future dollars).</summary>
    public decimal ProjectedValue { get; init; }

    /// <summary>Projected remaining mortgage balance, if the property has a linked liability (future dollars).</summary>
    public decimal ProjectedMortgageBalance { get; init; }

    /// <summary>Projected value minus the projected mortgage balance (future dollars). Informational —
    /// not counted toward retirement readiness since real estate equity isn't easily liquid.</summary>
    public decimal ProjectedEquity { get; init; }

    /// <summary>
    /// Net rental cash flow after vacancy, operating expenses, capital-expenditure reserve, and debt
    /// service (future dollars). Zero for a primary residence. Floored at zero — a cash-flow-negative
    /// rental isn't modelled as reducing guaranteed income in this version.
    /// </summary>
    public decimal AnnualNetCashFlow { get; init; }
}

/// <summary>Household-wide real estate projection.</summary>
public record RealEstateSummary
{
    public IReadOnlyList<RealEstateProjection> Properties { get; init; } = Array.Empty<RealEstateProjection>();

    /// <summary>Combined projected equity across every real estate account (future dollars).</summary>
    public decimal TotalProjectedEquity { get; init; }

    /// <summary>Combined net rental cash flow across every rental property (future dollars).</summary>
    public decimal TotalAnnualRentalIncome { get; init; }
}
