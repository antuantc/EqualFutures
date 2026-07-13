using System.Security.Claims;
using EqualFutures.Core.Analysis;
using EqualFutures.Domain;
using EqualFutures.Infrastructure.Data;
using Microsoft.AspNetCore.Components.Authorization;

namespace EqualFutures.Web.Services;

/// <summary>
/// Per-circuit holder for the signed-in user's plan and its computed analysis.
/// Keeps Blazor components free of data-access and calculation concerns.
/// </summary>
public class PlanState(
    AuthenticationStateProvider authProvider,
    IPlanService planService,
    IPlanAnalysisService analysis)
{
    public FinancialPlan? Plan { get; private set; }
    public PlanSummary? Summary { get; private set; }
    public bool IsLoaded => Plan is not null;

    /// <summary>Loads the current user's plan (seeding a sample on first use) and computes analysis.</summary>
    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (Plan is not null) return;
        await ReloadAsync(ct);
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        Plan = await planService.GetOrCreateAsync(userId, ct);
        Recompute();
    }

    /// <summary>Recomputes the analysis without a database round-trip (for live what-if editing).</summary>
    public void Recompute()
    {
        if (Plan is not null)
            Summary = analysis.Analyze(Plan);
    }

    /// <summary>Persists the plan and refreshes the analysis.</summary>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        if (Plan is null) return;
        await planService.SaveAsync(Plan, ct);
        Recompute();
    }

    private async Task<string> GetUserIdAsync()
    {
        var authState = await authProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("No authenticated user is available.");
        return userId;
    }
}
