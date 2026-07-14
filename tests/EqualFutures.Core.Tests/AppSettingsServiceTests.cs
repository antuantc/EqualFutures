using EqualFutures.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EqualFutures.Core.Tests;

public class AppSettingsServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<FinancialDbContext> _options;

    public AppSettingsServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<FinancialDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new FinancialDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    private FinancialDbContext NewContext() => new(_options);

    [Fact]
    public async Task GetAsync_NoRowYet_CreatesDefaultRow()
    {
        await using var ctx = NewContext();
        var service = new AppSettingsService(ctx);

        var settings = await service.GetAsync();

        Assert.True(settings.AllowNewRegistrations);
        Assert.Equal(1, await ctx.AppSettings.CountAsync());
    }

    [Fact]
    public async Task GetAsync_CalledTwice_ReturnsSameRow()
    {
        await using var ctx = NewContext();
        var service = new AppSettingsService(ctx);

        await service.GetAsync();
        await service.GetAsync();

        Assert.Equal(1, await ctx.AppSettings.CountAsync());
    }

    [Fact]
    public async Task SaveAsync_PersistsChangesAcrossContexts()
    {
        await using (var ctx = NewContext())
        {
            var service = new AppSettingsService(ctx);
            var settings = await service.GetAsync();
            settings.AllowNewRegistrations = false;
            settings.DefaultPlanningHorizonAge = 90;
            await service.SaveAsync(settings);
        }

        await using (var ctx = NewContext())
        {
            var service = new AppSettingsService(ctx);
            var settings = await service.GetAsync();

            Assert.False(settings.AllowNewRegistrations);
            Assert.Equal(90, settings.DefaultPlanningHorizonAge);
        }
    }

    [Fact]
    public async Task GetDefaultPlanAssumptionsAsync_MapsSettingsToAssumptions()
    {
        await using var ctx = NewContext();
        var service = new AppSettingsService(ctx);
        var settings = await service.GetAsync();
        settings.DefaultInflationRate = 0.02m;
        settings.DefaultPreRetirementReturn = 0.08m;
        await service.SaveAsync(settings);

        var assumptions = await service.GetDefaultPlanAssumptionsAsync();

        Assert.Equal(0.02m, assumptions.InflationRate);
        Assert.Equal(0.08m, assumptions.PreRetirementReturn);
    }

    public void Dispose() => _connection.Dispose();
}
