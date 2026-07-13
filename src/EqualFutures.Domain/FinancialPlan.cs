using System.ComponentModel.DataAnnotations;

namespace EqualFutures.Domain;

/// <summary>
/// Aggregate root for a single household's plan. Owned by an Identity user
/// (<see cref="OwnerId"/>) so each signed-in user sees only their own family.
/// </summary>
public class FinancialPlan
{
    public int Id { get; set; }

    /// <summary>Identity user id (AspNetUsers.Id) that owns this plan.</summary>
    [Required]
    public string OwnerId { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string HouseholdName { get; set; } = "My Family";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public PlanAssumptions Assumptions { get; set; } = new();

    /// <summary>The lens the household currently uses to judge fairness across children.</summary>
    public FairnessMetric PreferredFairnessMetric { get; set; } = FairnessMetric.EqualInflationAdjustedValue;

    public List<Parent> Parents { get; set; } = new();
    public List<Child> Children { get; set; } = new();
    public List<Account> Accounts { get; set; } = new();
    public List<Liability> Liabilities { get; set; } = new();
}
