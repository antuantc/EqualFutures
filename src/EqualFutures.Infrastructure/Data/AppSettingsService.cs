using EqualFutures.Domain;
using Microsoft.EntityFrameworkCore;

namespace EqualFutures.Infrastructure.Data;

/// <summary>Reads and updates the single admin-configurable settings row.</summary>
public interface IAppSettingsService
{
    /// <summary>Returns the current settings, creating the default row on first use.</summary>
    Task<AppSettings> GetAsync(CancellationToken ct = default);

    /// <summary>Persists updated settings.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);

    /// <summary>Builds the <see cref="PlanAssumptions"/> a brand-new plan should start with.</summary>
    Task<PlanAssumptions> GetDefaultPlanAssumptionsAsync(CancellationToken ct = default);
}

public class AppSettingsService(FinancialDbContext db) : IAppSettingsService
{
    public async Task<AppSettings> GetAsync(CancellationToken ct = default)
    {
        var settings = await db.AppSettings.FirstOrDefaultAsync(ct);
        if (settings is not null) return settings;

        settings = new AppSettings();
        db.AppSettings.Add(settings);
        await db.SaveChangesAsync(ct);
        return settings;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        var existing = await db.AppSettings.FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            db.AppSettings.Add(settings);
        }
        else
        {
            existing.AllowNewRegistrations = settings.AllowNewRegistrations;
            existing.DefaultInflationRate = settings.DefaultInflationRate;
            existing.DefaultEducationInflationRate = settings.DefaultEducationInflationRate;
            existing.DefaultPreRetirementReturn = settings.DefaultPreRetirementReturn;
            existing.DefaultPostRetirementReturn = settings.DefaultPostRetirementReturn;
            existing.DefaultRealEstateAppreciationRate = settings.DefaultRealEstateAppreciationRate;
            existing.DefaultSafeWithdrawalRate = settings.DefaultSafeWithdrawalRate;
            existing.DefaultPlanningHorizonAge = settings.DefaultPlanningHorizonAge;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<PlanAssumptions> GetDefaultPlanAssumptionsAsync(CancellationToken ct = default)
    {
        var settings = await GetAsync(ct);
        return new PlanAssumptions
        {
            InflationRate = settings.DefaultInflationRate,
            EducationInflationRate = settings.DefaultEducationInflationRate,
            PreRetirementReturn = settings.DefaultPreRetirementReturn,
            PostRetirementReturn = settings.DefaultPostRetirementReturn,
            RealEstateAppreciationRate = settings.DefaultRealEstateAppreciationRate,
            SafeWithdrawalRate = settings.DefaultSafeWithdrawalRate,
            PlanningHorizonAge = settings.DefaultPlanningHorizonAge
        };
    }
}
