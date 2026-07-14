using EqualFutures.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EqualFutures.Infrastructure;

/// <summary>Registers the financial data context and plan services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Migrations history table name for the financial context (keeps it distinct from Identity's).</summary>
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Financial";

    public static IServiceCollection AddEqualFuturesInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FinancialDbContext>(options =>
            options.UseSqlite(connectionString, sqlite =>
                sqlite.MigrationsHistoryTable(MigrationsHistoryTable)));

        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IFamilyService, FamilyService>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        return services;
    }
}
