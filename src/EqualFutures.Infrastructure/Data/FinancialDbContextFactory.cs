using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EqualFutures.Infrastructure.Data;

/// <summary>
/// Enables `dotnet ef migrations` for the financial context at design time without a
/// running host. Uses the same SQLite database file the web app uses.
/// </summary>
public class FinancialDbContextFactory : IDesignTimeDbContextFactory<FinancialDbContext>
{
    public FinancialDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FinancialDbContext>()
            .UseSqlite("Data Source=app.db", sqlite =>
                sqlite.MigrationsHistoryTable(ServiceCollectionExtensions.MigrationsHistoryTable))
            .Options;
        return new FinancialDbContext(options);
    }
}
