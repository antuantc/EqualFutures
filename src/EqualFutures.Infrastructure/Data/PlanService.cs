using EqualFutures.Domain;
using Microsoft.EntityFrameworkCore;

namespace EqualFutures.Infrastructure.Data;

/// <summary>Loads, seeds, and persists the household plan a signed-in user belongs to.</summary>
public interface IPlanService
{
    /// <summary>
    /// Returns the plan the user belongs to, creating a new plan (with the user as
    /// owner) on first access. Resolution is by <see cref="PlanMember"/>, so invited
    /// spouses and children see the shared family plan — never someone else's.
    /// </summary>
    Task<FinancialPlan> GetOrCreateAsync(string userId, string? userEmail = null, CancellationToken ct = default);

    /// <summary>Replaces the plan contents with the demo sample household. Requires edit rights.</summary>
    Task<FinancialPlan> LoadSampleDataAsync(string userId, string? userEmail = null, CancellationToken ct = default);

    /// <summary>Removes all people, accounts, and liabilities from the plan. Requires edit rights.</summary>
    Task<FinancialPlan> ClearDataAsync(string userId, string? userEmail = null, CancellationToken ct = default);

    /// <summary>Persists changes to an existing plan. Requires edit rights.</summary>
    Task SaveAsync(FinancialPlan plan, string userId, CancellationToken ct = default);
}

public class PlanService(FinancialDbContext db, IAppSettingsService appSettings) : IPlanService
{
    public async Task<FinancialPlan> GetOrCreateAsync(string userId, string? userEmail = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Most recently joined plan the user is a member of.
        var planId = await db.PlanMembers
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.JoinedUtc)
            .Select(m => (int?)m.FinancialPlanId)
            .FirstOrDefaultAsync(ct);

        // Backfill: plans created before membership existed have an OwnerId but no member row.
        if (planId is null)
        {
            var legacy = await db.Plans
                .Where(p => p.OwnerId == userId)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync(ct);

            if (legacy is not null)
            {
                db.PlanMembers.Add(new PlanMember
                {
                    FinancialPlanId = legacy.Id,
                    UserId = userId,
                    Email = userEmail ?? string.Empty,
                    Role = PlanRole.Owner
                });
                await db.SaveChangesAsync(ct);
                planId = legacy.Id;
            }
        }

        // A user who registered directly (rather than clicking the join link) may
        // still have a pending invitation waiting for their email — link it now
        // instead of giving them a disconnected, brand-new household.
        if (planId is null && !string.IsNullOrWhiteSpace(userEmail))
        {
            var normalizedEmail = userEmail.Trim().ToLowerInvariant();
            var invitation = await db.PlanInvitations
                .Where(i => i.Email == normalizedEmail && i.Status == InvitationStatus.Pending)
                .OrderByDescending(i => i.CreatedUtc)
                .FirstOrDefaultAsync(ct);

            if (invitation is not null && invitation.IsPending(DateTime.UtcNow))
            {
                db.PlanMembers.Add(new PlanMember
                {
                    FinancialPlanId = invitation.FinancialPlanId,
                    UserId = userId,
                    Email = normalizedEmail,
                    Role = invitation.Role,
                    JoinedUtc = DateTime.UtcNow
                });
                invitation.Status = InvitationStatus.Accepted;
                invitation.AcceptedByUserId = userId;
                invitation.AcceptedUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                planId = invitation.FinancialPlanId;
            }
        }

        if (planId is not null)
            return await LoadByIdAsync(planId.Value, ct)
                   ?? throw new InvalidOperationException("Plan membership references a missing plan.");

        // Brand-new user: create their own plan and make them the owner.
        var plan = new FinancialPlan
        {
            OwnerId = userId,
            HouseholdName = "My Household",
            Assumptions = await appSettings.GetDefaultPlanAssumptionsAsync(ct)
        };
        plan.Members.Add(new PlanMember { UserId = userId, Email = userEmail ?? string.Empty, Role = PlanRole.Owner });
        db.Plans.Add(plan);
        await db.SaveChangesAsync(ct);
        return await LoadByIdAsync(plan.Id, ct) ?? plan;
    }

    public async Task<FinancialPlan> LoadSampleDataAsync(string userId, string? userEmail = null, CancellationToken ct = default)
    {
        var plan = await GetOrCreateAsync(userId, userEmail, ct);
        RequireEdit(plan, userId);

        RemoveChildren(plan);
        SamplePlanFactory.Populate(plan);
        await db.SaveChangesAsync(ct);

        // Child ids and liability ids are only known after the first save; link 529
        // accounts and real estate mortgages now.
        bool needsSecondSave = SamplePlanFactory.LinkEducationBeneficiaries(plan);
        needsSecondSave |= SamplePlanFactory.LinkRealEstateMortgages(plan);
        if (needsSecondSave)
            await db.SaveChangesAsync(ct);

        return plan;
    }

    public async Task<FinancialPlan> ClearDataAsync(string userId, string? userEmail = null, CancellationToken ct = default)
    {
        var plan = await GetOrCreateAsync(userId, userEmail, ct);
        RequireEdit(plan, userId);

        RemoveChildren(plan);
        await db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task SaveAsync(FinancialPlan plan, string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        RequireEdit(plan, userId);

        plan.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private Task<FinancialPlan?> LoadByIdAsync(int planId, CancellationToken ct) =>
        db.Plans
            .AsSplitQuery()
            .Include(p => p.Parents)
            .Include(p => p.Children)
            .Include(p => p.Accounts)
            .Include(p => p.Liabilities)
            .Include(p => p.Members)
            .Include(p => p.Invitations)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

    private static void RequireEdit(FinancialPlan plan, string userId)
    {
        var member = plan.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null || !member.CanEdit)
            throw new UnauthorizedAccessException("You do not have permission to edit this plan.");
    }

    /// <summary>Deletes all dependent rows so the plan can be repopulated or left empty.</summary>
    private void RemoveChildren(FinancialPlan plan)
    {
        db.Parents.RemoveRange(plan.Parents);
        db.Children.RemoveRange(plan.Children);
        db.Accounts.RemoveRange(plan.Accounts);
        db.Liabilities.RemoveRange(plan.Liabilities);
        plan.Parents.Clear();
        plan.Children.Clear();
        plan.Accounts.Clear();
        plan.Liabilities.Clear();
    }
}
