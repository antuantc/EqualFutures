using EqualFutures.Core.Financials;
using EqualFutures.Core.RealEstate;
using EqualFutures.Core.Retirement;
using EqualFutures.Domain;

namespace EqualFutures.Core.Tests;

public class RetirementCalculatorTests
{
    private readonly RetirementCalculator _calc = new(new RealEstateCalculator());

    private static DateOnly BirthDateForAge(int age) => new(DateTime.UtcNow.Year - age, 1, 1);

    /// <summary>
    /// Independently derives the expected required nest egg the same way the
    /// calculator should: as the present value of the (inflation-adjusted) portfolio
    /// need for the retirement duration, discounted at the real post-retirement return.
    /// </summary>
    private static decimal ExpectedRequiredNestEgg(decimal annualPortfolioNeed, PlanAssumptions assumptions, int retirementAge)
    {
        int duration = Math.Max(0, assumptions.PlanningHorizonAge - retirementAge);
        decimal realRate = FinancialMath.RealRate(assumptions.PostRetirementReturn, assumptions.InflationRate);
        return FinancialMath.PresentValueOfAnnuityDue(annualPortfolioNeed, realRate, duration);
    }

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
        Assert.Equal(65, r.RetirementAge);
        Assert.Equal(630_000m, Math.Round(r.ProjectedNestEgg, 0));      // 600k * 1.05
        Assert.Equal(20_600m, Math.Round(r.AnnualSpendingNeed, 0));      // 20k * 1.03
        Assert.Equal(ExpectedRequiredNestEgg(20_600m, plan.Assumptions, retirementAge: 65), r.RequiredNestEgg);
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
        Assert.Equal(ExpectedRequiredNestEgg(20_000m, plan.Assumptions, retirementAge: 65), r.RequiredNestEgg);
    }

    [Fact]
    public void Project_HigherPostRetirementReturn_LowersRequiredNestEgg()
    {
        FinancialPlan PlanWithReturn(decimal postRetirementReturn) => new()
        {
            Assumptions = new PlanAssumptions { InflationRate = 0.03m, PreRetirementReturn = 0.05m, PostRetirementReturn = postRetirementReturn, PlanningHorizonAge = 95 },
            ExpectedAnnualRetirementSpending = 40_000m,
            Parents = { new Parent { BirthDate = BirthDateForAge(65), PlannedRetirementAge = 65 } },
            Accounts = { new Account { Category = AccountCategory.Investment, CurrentBalance = 1_000_000m } }
        };

        var lowReturn = _calc.Project(PlanWithReturn(0.03m));
        var highReturn = _calc.Project(PlanWithReturn(0.07m));

        Assert.True(highReturn.RequiredNestEgg < lowReturn.RequiredNestEgg);
    }

    [Fact]
    public void Project_LongerPlanningHorizon_RaisesRequiredNestEgg()
    {
        FinancialPlan PlanWithHorizon(int planningHorizonAge) => new()
        {
            Assumptions = new PlanAssumptions { InflationRate = 0.03m, PreRetirementReturn = 0.05m, PostRetirementReturn = 0.05m, PlanningHorizonAge = planningHorizonAge },
            ExpectedAnnualRetirementSpending = 40_000m,
            Parents = { new Parent { BirthDate = BirthDateForAge(65), PlannedRetirementAge = 65 } },
            Accounts = { new Account { Category = AccountCategory.Investment, CurrentBalance = 1_000_000m } }
        };

        var shortHorizon = _calc.Project(PlanWithHorizon(85));
        var longHorizon = _calc.Project(PlanWithHorizon(100));

        Assert.True(longHorizon.RequiredNestEgg > shortHorizon.RequiredNestEgg);
    }

    [Fact]
    public void Project_GrowthTrajectory_ExtendsThroughPlanningHorizonAge()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { InflationRate = 0.03m, PreRetirementReturn = 0.05m, PostRetirementReturn = 0.05m, PlanningHorizonAge = 90 },
            ExpectedAnnualRetirementSpending = 40_000m,
            Parents = { new Parent { BirthDate = BirthDateForAge(60), PlannedRetirementAge = 65 } },
            Accounts = { new Account { Category = AccountCategory.Investment, CurrentBalance = 500_000m, AnnualContribution = 20_000m } }
        };

        var r = _calc.Project(plan);

        Assert.Equal(65, r.RetirementAge);
        var lastPoint = r.GrowthTrajectory[^1];
        Assert.Equal(90, lastPoint.Age);                    // extends all the way to Plan until age
        Assert.True(r.GrowthTrajectory.Count > r.YearsToRetirement + 1); // includes drawdown years, not just accumulation
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

    [Fact]
    public void Project_RentalIncome_AddsToGuaranteedIncomeAndReducesPortfolioNeed()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { InflationRate = 0m, PreRetirementReturn = 0m, SafeWithdrawalRate = 0.04m },
            ExpectedAnnualRetirementSpending = 50_000m,
            Parents = { new Parent { BirthDate = BirthDateForAge(65), PlannedRetirementAge = 65 } },
            Accounts =
            {
                new Account { Category = AccountCategory.Investment, CurrentBalance = 500_000m },
                new Account
                {
                    Category = AccountCategory.RealEstate,
                    CurrentBalance = 200_000m,
                    Use = RealEstateUse.Rental,
                    MonthlyRentToday = 2_000m,
                    MonthlyOperatingExpenses = 300m
                }
            }
        };

        var r = _calc.Project(plan);

        // Gross rent 24,000 - expenses 3,600 = 20,400 net (years=0, no inflation, no mortgage).
        Assert.Equal(20_400m, r.AnnualRealEstateIncome);
        Assert.Equal(20_400m, r.AnnualGuaranteedIncome);
        Assert.Equal(29_600m, r.AnnualPortfolioNeed); // 50,000 - 20,400
    }

    [Fact]
    public void Project_PrimaryResidenceEquity_IsInformationalAndExcludedFromNestEgg()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { InflationRate = 0m, PreRetirementReturn = 0m, RealEstateAppreciationRate = 0m },
            ExpectedAnnualRetirementSpending = 10_000m,
            Parents = { new Parent { BirthDate = BirthDateForAge(65), PlannedRetirementAge = 65 } },
            Accounts =
            {
                new Account { Category = AccountCategory.Investment, CurrentBalance = 100_000m },
                new Account { Category = AccountCategory.RealEstate, CurrentBalance = 500_000m, Use = RealEstateUse.PrimaryResidence }
            }
        };

        var r = _calc.Project(plan);

        Assert.Equal(500_000m, r.ProjectedRealEstateEquity);
        Assert.Equal(100_000m, r.ProjectedNestEgg); // home equity not folded in
        Assert.Equal(0m, r.AnnualRealEstateIncome);  // primary residence generates no income
    }
}
