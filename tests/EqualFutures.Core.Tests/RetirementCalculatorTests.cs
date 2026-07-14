using EqualFutures.Core.Retirement;
using EqualFutures.Domain;

namespace EqualFutures.Core.Tests;

public class RetirementCalculatorTests
{
    private readonly RetirementCalculator _calc = new();

    private static DateOnly BirthDateForAge(int age) => new(DateTime.UtcNow.Year - age, 1, 1);

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
            ExpectedAnnualRetirementSpending = 20_000m,
            Parents = { new Parent { Name = "A", BirthDate = BirthDateForAge(64), PlannedRetirementAge = 65 } },
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
            ExpectedAnnualRetirementSpending = 60_000m,
            Parents = { new Parent { BirthDate = BirthDateForAge(60), PlannedRetirementAge = 65 } },
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
            ExpectedAnnualRetirementSpending = 50_000m,
            Parents = { new Parent { BirthDate = BirthDateForAge(65), PlannedRetirementAge = 65, SocialSecurityClaimingAge = 65, EstimatedAnnualSocialSecurity = 30_000m } },
            Accounts = { new Account { Category = AccountCategory.Investment, CurrentBalance = 500_000m } }
        };

        var r = _calc.Project(plan);

        Assert.Equal(20_000m, r.AnnualPortfolioNeed);       // 50k spending - 30k SS
        Assert.Equal(500_000m, r.RequiredNestEgg);          // 20k / 0.04
    }

    [Fact]
    public void Project_SocialSecurityExcludedBeforeClaimingAge()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { InflationRate = 0m, PreRetirementReturn = 0m, SafeWithdrawalRate = 0.04m },
            ExpectedAnnualRetirementSpending = 50_000m,
            // Retires at 60, but Social Security can't be claimed until 67.
            Parents = { new Parent { BirthDate = BirthDateForAge(60), PlannedRetirementAge = 60, SocialSecurityClaimingAge = 67, EstimatedAnnualSocialSecurity = 30_000m } },
            Accounts = { new Account { Category = AccountCategory.Investment, CurrentBalance = 500_000m } }
        };

        var r = _calc.Project(plan);

        Assert.Equal(0m, r.AnnualGuaranteedIncome);
        Assert.Equal(50_000m, r.AnnualPortfolioNeed);       // full spending, no SS yet
    }

    [Fact]
    public void Project_SocialSecurityClaimingAgeCannotGoBelow62()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { InflationRate = 0m, PreRetirementReturn = 0m, SafeWithdrawalRate = 0.04m },
            ExpectedAnnualRetirementSpending = 50_000m,
            // Even if a bad/legacy claiming age of 55 is stored, SS still can't count before 62.
            Parents = { new Parent { BirthDate = BirthDateForAge(60), PlannedRetirementAge = 60, SocialSecurityClaimingAge = 55, EstimatedAnnualSocialSecurity = 30_000m } },
            Accounts = { new Account { Category = AccountCategory.Investment, CurrentBalance = 500_000m } }
        };

        var r = _calc.Project(plan);

        Assert.Equal(0m, r.AnnualGuaranteedIncome);
    }
}
