using System.ComponentModel.DataAnnotations;

namespace EqualFutures.Domain;

/// <summary>
/// An invitation for someone to join a household plan. Because the app has no email
/// service, the owner shares the generated join link (built from <see cref="Token"/>).
/// </summary>
public class PlanInvitation
{
    public int Id { get; set; }
    public int FinancialPlanId { get; set; }

    /// <summary>Email the invitation was issued to. The accepting user's email must match.</summary>
    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public PlanRole Role { get; set; } = PlanRole.Adult;

    /// <summary>Cryptographically random, URL-safe token embedded in the join link.</summary>
    [Required, MaxLength(128)]
    public string Token { get; set; } = string.Empty;

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    /// <summary>Identity user id of the owner who created the invitation.</summary>
    [Required]
    public string InvitedByUserId { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow.AddDays(14);

    public string? AcceptedByUserId { get; set; }
    public DateTime? AcceptedUtc { get; set; }

    public bool IsPending(DateTime nowUtc) => Status == InvitationStatus.Pending && ExpiresUtc > nowUtc;
}
