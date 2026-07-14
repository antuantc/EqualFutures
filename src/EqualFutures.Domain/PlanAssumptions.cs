namespace EqualFutures.Domain;

/// <summary>
/// Economic and market assumptions that drive every projection. Kept on the
/// plan so scenarios can vary them without touching calculation code.
/// </summary>
public class PlanAssumptions
{
    /// <summary>General price inflation, e.g. 0.03 for 3%.</summary>
    public decimal InflationRate { get; set; } = 0.03m;

    /// <summary>College cost inflation, historically higher than general inflation.</summary>
    public decimal EducationInflationRate { get; set; } = 0.05m;

    /// <summary>Expected nominal annual investment return before retirement.</summary>
    public decimal PreRetirementReturn { get; set; } = 0.07m;

    /// <summary>Expected nominal annual investment return during retirement (typically more conservative).</summary>
    public decimal PostRetirementReturn { get; set; } = 0.05m;

    /// <summary>
    /// Default appreciation rate for real estate accounts, used when an account doesn't
    /// set its own <see cref="Account.ExpectedReturnOverride"/>. Real estate historically
    /// appreciates slower than equities, so this is kept separate from PreRetirementReturn.
    /// </summary>
    public decimal RealEstateAppreciationRate { get; set; } = 0.035m;

    /// <summary>Safe withdrawal rate for retirement (the "4% rule" default).</summary>
    public decimal SafeWithdrawalRate { get; set; } = 0.04m;

    /// <summary>Age to which retirement funding must last.</summary>
    public int PlanningHorizonAge { get; set; } = 95;
}
