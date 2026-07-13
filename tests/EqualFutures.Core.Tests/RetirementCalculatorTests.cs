using EqualFutures.Core.Retirement;
using EqualFutures.Domain;

namespace EqualFutures.Core.Tests;

public class RetirementCalculatorTests
{
    private readonly RetirementCalculator _calc = new();

    [Fact]
    public void Project_NoParents_ReturnsEmptyProjection()
    {
        var result = _calc.Project(new FinancialPlan());
        Assert.Equal(0, result.YearsToRetirement);
        Assert.Equal(0m, result.ProjectedNestEgg);
    }

    [Fact]
    public void Project_ComputesNestEggAndRequirement()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions
            {
                InflationRate = 0.03m,
                PreRetirementReturn = 0.05m,
                SafeWithdrawalRate = 0.04m
            },
            Parents =
            {
                new Parent { Name = "A", CurrentAge = 64, PlannedRetirementAge = 65, ExpectedAnnualRetirementSpending = 20_000m }
            },
            Accounts =
            {
                new Account { Category = AccountCategory.Investment, CurrentBalance = 600_000m, AnnualContribution = 0m }
            }
        };

        var r = _calc.Project(plan);

        Assert.Equal(1, r.YearsToRetirement);
        Assert.Equal(630_000m, Math.Round(r.ProjectedNestEgg, 0));      // 600k * 1.05
        Assert.Equal(20_600m, Math.Round(r.AnnualSpendingNeed, 0));      // 20k * 1.03
        Assert.Equal(515_000m, Math.Round(r.RequiredNestEgg, 0));        // 20.6k / 0.04
        Assert.True(r.OnTrack);
        Assert.Equal(100m, r.ReadinessScore);                           // capped at 100
    }

    [Fact]
    public void Project_ShortfallIsFlagged()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { InflationRate = 0.03m, PreRetirementReturn = 0.07m, SafeWithdrawalRate = 0.04m },
            Parents = { new Parent { CurrentAge = 60, PlannedRetirementAge = 65, ExpectedAnnualRetirementSpending = 60_000m } },
            Accounts = { new Account { Category = AccountCategory.Investment, CurrentBalance = 100_000m } }
        };

        var r = _calc.Project(plan);

        Assert.False(r.OnTrack);
        Assert.True(r.FundingGap < 0);
    }

    [Fact]
    public void Project_GuaranteedIncomeReducesPortfolioNeed()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { InflationRate = 0m, PreRetirementReturn = 0m, SafeWithdrawalRate = 0.04m },
            Parents =
            {
                new Parent { CurrentAge = 65, PlannedRetirementAge = 65, ExpectedAnnualRetirementSpending = 50_000m, EstimatedAnnualSocialSecurity = 30_000m }
            },
            Accounts = { new Account { Category = AccountCategory.Investment, CurrentBalance = 500_000m } }
        };

        var r = _calc.Project(plan);

        Assert.Equal(20_000m, r.AnnualPortfolioNeed);       // 50k spending - 30k SS
        Assert.Equal(500_000m, r.RequiredNestEgg);          // 20k / 0.04
    }
}
