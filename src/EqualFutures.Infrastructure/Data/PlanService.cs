using EqualFutures.Domain;
using Microsoft.EntityFrameworkCore;

namespace EqualFutures.Infrastructure.Data;

/// <summary>Loads, seeds, and persists the household plan owned by a signed-in user.</summary>
public interface IPlanService
{
    /// <summary>Returns the user's plan, creating a seeded sample plan on first access.</summary>
    Task<FinancialPlan> GetOrCreateAsync(string ownerId, CancellationToken ct = default);

    /// <summary>Persists changes to an existing plan.</summary>
    Task SaveAsync(FinancialPlan plan, CancellationToken ct = default);
}

public class PlanService(FinancialDbContext db) : IPlanService
{
    public async Task<FinancialPlan> GetOrCreateAsync(string ownerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var plan = await db.Plans
            .AsSplitQuery()
            .Include(p => p.Parents)
            .Include(p => p.Children)
            .Include(p => p.Accounts)
            .Include(p => p.Liabilities)
            .FirstOrDefaultAsync(p => p.OwnerId == ownerId, ct);

        if (plan is not null) return plan;

        plan = SamplePlanFactory.Create(ownerId);
        db.Plans.Add(plan);
        await db.SaveChangesAsync(ct);

        // Child ids are only known after the first save; link 529 accounts now.
        if (SamplePlanFactory.LinkEducationBeneficiaries(plan))
            await db.SaveChangesAsync(ct);

        return plan;
    }

    public async Task SaveAsync(FinancialPlan plan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
