using EqualFutures.Core.Education;
using EqualFutures.Core.Fairness;
using EqualFutures.Domain;

namespace EqualFutures.Core.Tests;

public class FairnessEngineTests
{
    private readonly FairnessEngine _engine = new();

    private static EducationProjection Projection(int id, string name, decimal familyContribution, decimal totalCost, int yearsUntil = 0) =>
        new()
        {
            ChildId = id,
            ChildName = name,
            YearsUntilCollege = yearsUntil,
            TotalCostFutureDollars = totalCost,
            DesiredFamilyContribution = familyContribution
        };

    [Fact]
    public void EqualContributions_ScorePerfect()
    {
        var plan = new FinancialPlan();
        var projections = new[]
        {
            Projection(1, "A", 50_000m, 60_000m),
            Projection(2, "B", 50_000m, 60_000m)
        };

        var result = _engine.Evaluate(plan, projections, FairnessMetric.EqualDollarAmount);

        Assert.Equal(100m, result.FairnessScore);
        Assert.True(result.IsBalanced);
    }

    [Fact]
    public void UnequalContributions_LowerScore()
    {
        var plan = new FinancialPlan();
        var projections = new[]
        {
            Projection(1, "A", 40_000m, 60_000m),
            Projection(2, "B", 60_000m, 60_000m)
        };

        var result = _engine.Evaluate(plan, projections, FairnessMetric.EqualDollarAmount);

        // mean 50k, max deviation 10k => 100 * (1 - 10k/50k) = 80
        Assert.Equal(80m, result.FairnessScore);
        Assert.False(result.IsBalanced);
        Assert.Equal(50_000m, result.EqualTarget);
    }

    [Fact]
    public void ChildAssignedRetirementAccounts_CountTowardDollarFairness()
    {
        var plan = new FinancialPlan();
        plan.Accounts.Add(new Account
        {
            Name = "A custodial Roth IRA",
            Category = AccountCategory.Investment,
            BeneficiaryChildId = 1,
            CurrentBalance = 15_000m
        });
        plan.Accounts.Add(new Account
        {
            Name = "Unassigned 401(k)",
            Category = AccountCategory.Investment,
            CurrentBalance = 500_000m
        });
        var projections = new[]
        {
            Projection(1, "A", 50_000m, 60_000m),
            Projection(2, "B", 50_000m, 60_000m)
        };

        var result = _engine.Evaluate(plan, projections, FairnessMetric.EqualDollarAmount);

        Assert.Equal(65_000m, result.Children.Single(c => c.ChildName == "A").Value);
        Assert.Equal(50_000m, result.Children.Single(c => c.ChildName == "B").Value);
        Assert.Equal(57_500m, result.EqualTarget);
        Assert.False(result.IsBalanced);
    }

    [Fact]
    public void PercentOfTuition_UsesRatios()
    {
        var plan = new FinancialPlan();
        var projections = new[]
        {
            Projection(1, "A", 30_000m, 60_000m),   // 50%
            Projection(2, "B", 45_000m, 60_000m)    // 75%
        };

        var result = _engine.Evaluate(plan, projections, FairnessMetric.EqualPercentOfTuition);

        Assert.All(result.Children, c => Assert.True(c.IsRatio));
        Assert.Equal(0.5m, result.Children.Single(c => c.ChildName == "A").Value);
        Assert.Equal(0.75m, result.Children.Single(c => c.ChildName == "B").Value);
    }

    [Fact]
    public void SingleChild_IsAlwaysBalanced()
    {
        var plan = new FinancialPlan();
        var projections = new[] { Projection(1, "Only", 25_000m, 40_000m) };

        var result = _engine.Evaluate(plan, projections, FairnessMetric.EqualDollarAmount);

        Assert.Equal(100m, result.FairnessScore);
    }
}
