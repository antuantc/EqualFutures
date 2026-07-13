using EqualFutures.Domain;
using Microsoft.EntityFrameworkCore;

namespace EqualFutures.Infrastructure.Data;

/// <summary>
/// EF Core context for the financial-planning domain. Kept separate from the
/// Identity context so authentication and domain data evolve independently, while
/// sharing a single SQLite database file.
/// </summary>
public class FinancialDbContext(DbContextOptions<FinancialDbContext> options) : DbContext(options)
{
    public DbSet<FinancialPlan> Plans => Set<FinancialPlan>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Liability> Liabilities => Set<Liability>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<FinancialPlan>(e =>
        {
            e.HasIndex(p => p.OwnerId);
            e.OwnsOne(p => p.Assumptions);
            e.HasMany(p => p.Parents).WithOne().HasForeignKey(x => x.FinancialPlanId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Children).WithOne().HasForeignKey(x => x.FinancialPlanId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Accounts).WithOne().HasForeignKey(x => x.FinancialPlanId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Liabilities).WithOne().HasForeignKey(x => x.FinancialPlanId).OnDelete(DeleteBehavior.Cascade);
        });

        // Store monetary values with fixed precision.
        foreach (var property in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }
    }
}
