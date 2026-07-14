using EqualFutures.Domain;
using EqualFutures.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EqualFutures.Core.Tests;

/// <summary>
/// Integration tests for family membership and invitations, using a real SQLite
/// in-memory database so EF behaviour (cascades, indexes) is exercised.
/// </summary>
public class FamilyServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<FinancialDbContext> _options;

    public FamilyServiceTests()
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
    public async Task GetOrCreate_NewUser_BecomesOwnerMember()
    {
        await using var ctx = NewContext();
        var plans = new PlanService(ctx, new AppSettingsService(ctx));

        var plan = await plans.GetOrCreateAsync("user-1", "owner@example.com");

        var member = Assert.Single(plan.Members);
        Assert.Equal("user-1", member.UserId);
        Assert.Equal(PlanRole.Owner, member.Role);
        Assert.True(member.CanManageFamily);
    }

    [Fact]
    public async Task Invite_ThenAccept_SharesTheSamePlan()
    {
        int planId;
        await using (var ctx = NewContext())
        {
            var plans = new PlanService(ctx, new AppSettingsService(ctx));
            var family = new FamilyService(ctx);
            var plan = await plans.GetOrCreateAsync("owner", "owner@example.com");
            planId = plan.Id;

            var invite = await family.InviteAsync(planId, "owner", "Spouse@Example.com", PlanRole.Adult);
            Assert.False(string.IsNullOrWhiteSpace(invite.Token));

            var result = await family.AcceptAsync(invite.Token, "spouse", "spouse@example.com");
            Assert.Equal(AcceptOutcome.Success, result.Outcome);
        }

        // A fresh context: the invited user resolves to the owner's plan, not a new one.
        await using (var ctx = NewContext())
        {
            var plans = new PlanService(ctx, new AppSettingsService(ctx));
            var spousePlan = await plans.GetOrCreateAsync("spouse", "spouse@example.com");
            Assert.Equal(planId, spousePlan.Id);
            Assert.Equal(PlanRole.Adult, spousePlan.Members.Single(m => m.UserId == "spouse").Role);
        }
    }

    [Fact]
    public async Task Accept_WithWrongEmail_IsRejected()
    {
        await using var ctx = NewContext();
        var plans = new PlanService(ctx, new AppSettingsService(ctx));
        var family = new FamilyService(ctx);
        var plan = await plans.GetOrCreateAsync("owner", "owner@example.com");

        var invite = await family.InviteAsync(plan.Id, "owner", "kid@example.com", PlanRole.Child);
        var result = await family.AcceptAsync(invite.Token, "intruder", "intruder@example.com");

        Assert.Equal(AcceptOutcome.EmailMismatch, result.Outcome);
        Assert.False(await ctx.PlanMembers.AnyAsync(m => m.UserId == "intruder"));
    }

    [Fact]
    public async Task Accept_InvalidToken_IsRejected()
    {
        await using var ctx = NewContext();
        var family = new FamilyService(ctx);

        var result = await family.AcceptAsync("not-a-real-token", "user", "user@example.com");

        Assert.Equal(AcceptOutcome.InvalidOrExpired, result.Outcome);
    }

    [Fact]
    public async Task Invite_ByNonOwner_Throws()
    {
        await using var ctx = NewContext();
        var plans = new PlanService(ctx, new AppSettingsService(ctx));
        var family = new FamilyService(ctx);
        var plan = await plans.GetOrCreateAsync("owner", "owner@example.com");

        // Add an adult (non-owner) member directly.
        ctx.PlanMembers.Add(new PlanMember { FinancialPlanId = plan.Id, UserId = "adult", Email = "adult@example.com", Role = PlanRole.Adult });
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => family.InviteAsync(plan.Id, "adult", "someone@example.com", PlanRole.Adult));
    }

    [Fact]
    public async Task Child_CannotEditPlan()
    {
        await using var ctx = NewContext();
        var plans = new PlanService(ctx, new AppSettingsService(ctx));
        var family = new FamilyService(ctx);
        var plan = await plans.GetOrCreateAsync("owner", "owner@example.com");

        var invite = await family.InviteAsync(plan.Id, "owner", "kid@example.com", PlanRole.Child);
        await family.AcceptAsync(invite.Token, "kid", "kid@example.com");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => plans.ClearDataAsync("kid", "kid@example.com"));
    }

    [Fact]
    public async Task Accept_Twice_IsIdempotent()
    {
        await using var ctx = NewContext();
        var plans = new PlanService(ctx, new AppSettingsService(ctx));
        var family = new FamilyService(ctx);
        var plan = await plans.GetOrCreateAsync("owner", "owner@example.com");

        var invite = await family.InviteAsync(plan.Id, "owner", "spouse@example.com", PlanRole.Adult);

        var first = await family.AcceptAsync(invite.Token, "spouse", "spouse@example.com");
        var second = await family.AcceptAsync(invite.Token, "spouse", "spouse@example.com");

        Assert.Equal(AcceptOutcome.Success, first.Outcome);
        Assert.Equal(AcceptOutcome.AlreadyMember, second.Outcome);
        // Only one membership row is created despite two accepts.
        Assert.Equal(1, await ctx.PlanMembers.CountAsync(m => m.UserId == "spouse"));
    }

    public void Dispose() => _connection.Dispose();
}
