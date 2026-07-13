using System.Security.Claims;
using EqualFutures.Core.Analysis;
using EqualFutures.Domain;
using EqualFutures.Infrastructure.Data;
using Microsoft.AspNetCore.Components.Authorization;

namespace EqualFutures.Web.Services;

/// <summary>
/// Per-circuit holder for the signed-in user's plan, their role on it, and the
/// computed analysis. Keeps Blazor components free of data-access and calculation
/// concerns and centralizes the edit/manage permission checks.
/// </summary>
public class PlanState(
    AuthenticationStateProvider authProvider,
    IPlanService planService,
    IFamilyService familyService,
    IPlanAnalysisService analysis)
{
    public FinancialPlan? Plan { get; private set; }
    public PlanSummary? Summary { get; private set; }
    public PlanRole? Role { get; private set; }

    public string? UserId { get; private set; }
    public string? UserEmail { get; private set; }

    public bool IsLoaded => Plan is not null;

    /// <summary>Adults and owners may edit the plan; children have read-only access.</summary>
    public bool CanEdit => Role is PlanRole.Owner or PlanRole.Adult;

    /// <summary>Only the owner may invite/remove members.</summary>
    public bool CanManageFamily => Role == PlanRole.Owner;

    /// <summary>True when the loaded plan has no people, accounts, or liabilities yet.</summary>
    public bool IsEmpty => Plan is not null
        && Plan.Parents.Count == 0
        && Plan.Children.Count == 0
        && Plan.Accounts.Count == 0
        && Plan.Liabilities.Count == 0;

    /// <summary>Loads the current user's plan (creating one on first use) and computes analysis.</summary>
    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (Plan is not null) return;
        await ReloadAsync(ct);
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await CaptureUserAsync();
        Plan = await planService.GetOrCreateAsync(UserId!, UserEmail, ct);
        Role = Plan.Members.FirstOrDefault(m => m.UserId == UserId)?.Role;
        Recompute();
    }

    /// <summary>Populates the current user's plan with the demo sample household.</summary>
    public async Task LoadSampleDataAsync(CancellationToken ct = default)
    {
        if (!CanEdit) return;
        Plan = await planService.LoadSampleDataAsync(UserId!, UserEmail, ct);
        Role = Plan.Members.FirstOrDefault(m => m.UserId == UserId)?.Role;
        Recompute();
    }

    /// <summary>Removes all people, accounts, and liabilities from the current user's plan.</summary>
    public async Task ClearDataAsync(CancellationToken ct = default)
    {
        if (!CanEdit) return;
        Plan = await planService.ClearDataAsync(UserId!, UserEmail, ct);
        Role = Plan.Members.FirstOrDefault(m => m.UserId == UserId)?.Role;
        Recompute();
    }

    /// <summary>Recomputes the analysis without a database round-trip (for live what-if editing).</summary>
    public void Recompute()
    {
        if (Plan is not null)
            Summary = analysis.Analyze(Plan);
    }

    /// <summary>Persists the plan and refreshes the analysis. No-op for read-only members.</summary>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        if (Plan is null || !CanEdit) return;
        await planService.SaveAsync(Plan, ct);
        Recompute();
    }

    // ----- Family management -----

    public async Task<PlanInvitation> InviteAsync(string email, PlanRole role, CancellationToken ct = default)
    {
        var invitation = await familyService.InviteAsync(Plan!.Id, UserId!, email, role, ct);
        await ReloadAsync(ct);
        return invitation;
    }

    public async Task RevokeInvitationAsync(int invitationId, CancellationToken ct = default)
    {
        await familyService.RevokeInvitationAsync(invitationId, UserId!, ct);
        await ReloadAsync(ct);
    }

    public async Task RemoveMemberAsync(string targetUserId, CancellationToken ct = default)
    {
        await familyService.RemoveMemberAsync(Plan!.Id, targetUserId, UserId!, ct);
        await ReloadAsync(ct);
    }

    public async Task<AcceptResult> AcceptInvitationAsync(string token, CancellationToken ct = default)
    {
        await CaptureUserAsync();
        var result = await familyService.AcceptAsync(token, UserId!, UserEmail ?? string.Empty, ct);
        if (result.Outcome is AcceptOutcome.Success or AcceptOutcome.AlreadyMember)
        {
            Plan = null; // force the newly joined plan to be picked up
            await ReloadAsync(ct);
        }
        return result;
    }

    private async Task CaptureUserAsync()
    {
        var authState = await authProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        UserEmail = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;
        if (string.IsNullOrEmpty(UserId))
            throw new InvalidOperationException("No authenticated user is available.");
    }
}
