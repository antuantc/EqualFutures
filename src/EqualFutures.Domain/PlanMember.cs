using System.ComponentModel.DataAnnotations;

namespace EqualFutures.Domain;

/// <summary>
/// Links an Identity user to a shared <see cref="FinancialPlan"/> with a role.
/// A plan can have many members (parents, spouse, children), and a user can be
/// a member of more than one plan.
/// </summary>
public class PlanMember
{
    public int Id { get; set; }
    public int FinancialPlanId { get; set; }

    /// <summary>Identity user id (AspNetUsers.Id). Not an EF FK — Identity lives in a separate context.</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Email captured at join time, for display in the members list.</summary>
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public PlanRole Role { get; set; } = PlanRole.Adult;

    public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Adults and owners may edit the plan; children are read-only.</summary>
    public bool CanEdit => Role is PlanRole.Owner or PlanRole.Adult;

    /// <summary>Only owners may invite/remove members.</summary>
    public bool CanManageFamily => Role == PlanRole.Owner;
}
