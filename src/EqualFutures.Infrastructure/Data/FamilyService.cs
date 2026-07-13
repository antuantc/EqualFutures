using System.Security.Cryptography;
using EqualFutures.Domain;
using Microsoft.EntityFrameworkCore;

namespace EqualFutures.Infrastructure.Data;

/// <summary>Outcome of attempting to accept an invitation.</summary>
public enum AcceptOutcome
{
    Success,
    InvalidOrExpired,
    EmailMismatch,
    AlreadyMember
}

/// <summary>Result of an accept attempt, including the joined household when successful.</summary>
public record AcceptResult(AcceptOutcome Outcome, int? PlanId = null, string? HouseholdName = null);

/// <summary>Manages family membership and invitations for shared plans.</summary>
public interface IFamilyService
{
    /// <summary>The role a user holds on a plan, or null if they are not a member.</summary>
    Task<PlanRole?> GetRoleAsync(int planId, string userId, CancellationToken ct = default);

    /// <summary>Creates a pending invitation. Only the plan owner may invite.</summary>
    Task<PlanInvitation> InviteAsync(int planId, string requestingUserId, string email, PlanRole role, CancellationToken ct = default);

    /// <summary>Cancels a pending invitation. Only the plan owner may revoke.</summary>
    Task RevokeInvitationAsync(int invitationId, string requestingUserId, CancellationToken ct = default);

    /// <summary>Removes a member from the plan. Only the owner may remove, and never themselves.</summary>
    Task RemoveMemberAsync(int planId, string targetUserId, string requestingUserId, CancellationToken ct = default);

    /// <summary>Accepts an invitation by token for the signed-in user.</summary>
    Task<AcceptResult> AcceptAsync(string token, string userId, string userEmail, CancellationToken ct = default);
}

public class FamilyService(FinancialDbContext db) : IFamilyService
{
    public async Task<PlanRole?> GetRoleAsync(int planId, string userId, CancellationToken ct = default)
    {
        var member = await db.PlanMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.FinancialPlanId == planId && m.UserId == userId, ct);
        return member?.Role;
    }

    public async Task<PlanInvitation> InviteAsync(int planId, string requestingUserId, string email, PlanRole role, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        await RequireOwnerAsync(planId, requestingUserId, ct);

        // Owner cannot be assigned via invitation.
        if (role == PlanRole.Owner) role = PlanRole.Adult;

        var normalized = email.Trim().ToLowerInvariant();

        // Supersede any existing pending invitations for the same email on this plan.
        var existing = await db.PlanInvitations
            .Where(i => i.FinancialPlanId == planId && i.Email == normalized && i.Status == InvitationStatus.Pending)
            .ToListAsync(ct);
        foreach (var e in existing) e.Status = InvitationStatus.Revoked;

        var invitation = new PlanInvitation
        {
            FinancialPlanId = planId,
            Email = normalized,
            Role = role,
            Token = GenerateToken(),
            Status = InvitationStatus.Pending,
            InvitedByUserId = requestingUserId,
            CreatedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddDays(14)
        };
        db.PlanInvitations.Add(invitation);
        await db.SaveChangesAsync(ct);
        return invitation;
    }

    public async Task RevokeInvitationAsync(int invitationId, string requestingUserId, CancellationToken ct = default)
    {
        var invitation = await db.PlanInvitations.FirstOrDefaultAsync(i => i.Id == invitationId, ct);
        if (invitation is null) return;

        await RequireOwnerAsync(invitation.FinancialPlanId, requestingUserId, ct);
        invitation.Status = InvitationStatus.Revoked;
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(int planId, string targetUserId, string requestingUserId, CancellationToken ct = default)
    {
        await RequireOwnerAsync(planId, requestingUserId, ct);

        if (string.Equals(targetUserId, requestingUserId, StringComparison.Ordinal))
            throw new InvalidOperationException("The owner cannot remove themselves from the plan.");

        var member = await db.PlanMembers
            .FirstOrDefaultAsync(m => m.FinancialPlanId == planId && m.UserId == targetUserId, ct);
        if (member is null) return;

        if (member.Role == PlanRole.Owner)
            throw new InvalidOperationException("An owner cannot be removed.");

        db.PlanMembers.Remove(member);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AcceptResult> AcceptAsync(string token, string userId, string userEmail, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var invitation = await db.PlanInvitations.FirstOrDefaultAsync(i => i.Token == token, ct);
        if (invitation is null)
            return new AcceptResult(AcceptOutcome.InvalidOrExpired);

        var householdName = await db.Plans
            .Where(p => p.Id == invitation.FinancialPlanId)
            .Select(p => p.HouseholdName)
            .FirstOrDefaultAsync(ct);

        // Idempotent: if the user already belongs to this plan (e.g. a double
        // render or a refreshed link), report success rather than an error.
        var alreadyMember = await db.PlanMembers
            .AnyAsync(m => m.FinancialPlanId == invitation.FinancialPlanId && m.UserId == userId, ct);
        if (alreadyMember)
        {
            if (invitation.IsPending(DateTime.UtcNow))
            {
                invitation.Status = InvitationStatus.Accepted;
                invitation.AcceptedByUserId = userId;
                invitation.AcceptedUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            return new AcceptResult(AcceptOutcome.AlreadyMember, invitation.FinancialPlanId, householdName);
        }

        if (!invitation.IsPending(DateTime.UtcNow))
            return new AcceptResult(AcceptOutcome.InvalidOrExpired);

        // The link is bound to the invited email so a leaked link can't be used by someone else.
        if (!string.Equals(invitation.Email, (userEmail ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
            return new AcceptResult(AcceptOutcome.EmailMismatch);

        db.PlanMembers.Add(new PlanMember
        {
            FinancialPlanId = invitation.FinancialPlanId,
            UserId = userId,
            Email = (userEmail ?? string.Empty).Trim(),
            Role = invitation.Role,
            JoinedUtc = DateTime.UtcNow
        });
        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedByUserId = userId;
        invitation.AcceptedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new AcceptResult(AcceptOutcome.Success, invitation.FinancialPlanId, householdName);
    }

    private async Task RequireOwnerAsync(int planId, string userId, CancellationToken ct)
    {
        var role = await GetRoleAsync(planId, userId, ct);
        if (role != PlanRole.Owner)
            throw new UnauthorizedAccessException("Only the plan owner can manage the family.");
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
