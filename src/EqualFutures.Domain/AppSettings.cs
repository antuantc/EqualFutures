namespace EqualFutures.Domain;

/// <summary>
/// Single-row, admin-configurable defaults and feature toggles for the whole app.
/// Doesn't hold any household or user data — purely operational configuration.
/// </summary>
public class AppSettings
{
    /// <summary>Fixed singleton row id.</summary>
    public int Id { get; set; } = 1;

    /// <summary>When false, the registration page stops accepting new signups.</summary>
    public bool AllowNewRegistrations { get; set; } = true;

    // ----- Defaults applied to every brand-new plan's PlanAssumptions -----

    public decimal DefaultInflationRate { get; set; } = 0.03m;
    public decimal DefaultEducationInflationRate { get; set; } = 0.05m;
    public decimal DefaultPreRetirementReturn { get; set; } = 0.07m;
    public decimal DefaultPostRetirementReturn { get; set; } = 0.05m;
    public decimal DefaultRealEstateAppreciationRate { get; set; } = 0.035m;
    public decimal DefaultSafeWithdrawalRate { get; set; } = 0.04m;
    public int DefaultPlanningHorizonAge { get; set; } = 95;
}
