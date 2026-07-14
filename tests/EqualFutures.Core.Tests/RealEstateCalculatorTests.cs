using EqualFutures.Core.Financials;
using EqualFutures.Core.RealEstate;
using EqualFutures.Domain;

namespace EqualFutures.Core.Tests;

public class RealEstateCalculatorTests
{
    private readonly RealEstateCalculator _calc = new();

    [Fact]
    public void Project_PrimaryResidence_AppreciatesButHasNoCashFlow()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { RealEstateAppreciationRate = 0.04m },
            Accounts =
            {
                new Account
                {
                    Id = 1,
                    Name = "Home",
                    Category = AccountCategory.RealEstate,
                    CurrentBalance = 500_000m,
                    Use = RealEstateUse.PrimaryResidence
                }
            }
        };

        var result = _calc.Project(plan, years: 10);
        var home = Assert.Single(result.Properties);

        Assert.Equal(decimal.Round(500_000m * FinancialMath.FutureValue(1m, 0.04m, 10), 0), home.ProjectedValue);
        Assert.Equal(0m, home.AnnualNetCashFlow);
        Assert.Equal(home.ProjectedValue, home.ProjectedEquity); // no linked mortgage
        Assert.Equal(home.ProjectedEquity, result.TotalProjectedEquity);
        Assert.Equal(0m, result.TotalAnnualRentalIncome);
    }

    [Fact]
    public void Project_MortgagedProperty_NetsOutProjectedMortgageBalance()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { RealEstateAppreciationRate = 0m }, // isolate the mortgage effect
            Liabilities = { new Liability { Id = 7, Type = LiabilityType.Mortgage, CurrentBalance = 300_000m, InterestRate = 0.06m, MonthlyPayment = 1_798.65m } },
            Accounts =
            {
                new Account
                {
                    Id = 1,
                    Name = "Home",
                    Category = AccountCategory.RealEstate,
                    CurrentBalance = 500_000m,
                    Use = RealEstateUse.PrimaryResidence,
                    SecuredByLiabilityId = 7
                }
            }
        };

        var result = _calc.Project(plan, years: 10);
        var home = Assert.Single(result.Properties);

        var expectedMortgageBalance = FinancialMath.AmortizedBalance(300_000m, 0.06m, 1_798.65m, 120);
        Assert.Equal(decimal.Round(expectedMortgageBalance, 0), home.ProjectedMortgageBalance);
        Assert.Equal(500_000m - home.ProjectedMortgageBalance, home.ProjectedEquity);
        Assert.True(home.ProjectedMortgageBalance < 300_000m); // paid down over 10 years
    }

    [Fact]
    public void Project_Rental_ComputesNetCashFlowAfterExpensesAndDebtService()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { InflationRate = 0m, RealEstateAppreciationRate = 0m },
            Liabilities = { new Liability { Id = 5, Type = LiabilityType.Mortgage, CurrentBalance = 0m, InterestRate = 0.05m, MonthlyPayment = 500m } },
            Accounts =
            {
                new Account
                {
                    Id = 2,
                    Name = "Rental",
                    Category = AccountCategory.RealEstate,
                    CurrentBalance = 200_000m,
                    Use = RealEstateUse.Rental,
                    MonthlyRentToday = 2_000m,
                    VacancyRate = 0.10m,
                    MonthlyOperatingExpenses = 300m,
                    AnnualCapExReservePercent = 0.01m,
                    SecuredByLiabilityId = 5 // already paid off (CurrentBalance = 0)
                }
            }
        };

        var result = _calc.Project(plan, years: 0);
        var rental = Assert.Single(result.Properties);

        // Gross rent: 2000 * 12 * (1 - 0.10) = 21,600
        // Expenses: 300 * 12 = 3,600
        // CapEx reserve: 200,000 * 0.01 = 2,000
        // Mortgage already paid off -> no debt service
        // Net: 21,600 - 3,600 - 2,000 = 16,000
        Assert.Equal(16_000m, rental.AnnualNetCashFlow);
        Assert.Equal(16_000m, result.TotalAnnualRentalIncome);
    }

    [Fact]
    public void Project_RentalWithNegativeCashFlow_FloorsAtZero()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { InflationRate = 0m },
            Liabilities = { new Liability { Id = 9, Type = LiabilityType.Mortgage, CurrentBalance = 200_000m, InterestRate = 0.06m, MonthlyPayment = 2_500m } },
            Accounts =
            {
                new Account
                {
                    Id = 3,
                    Name = "Money-losing rental",
                    Category = AccountCategory.RealEstate,
                    CurrentBalance = 200_000m,
                    Use = RealEstateUse.Rental,
                    MonthlyRentToday = 1_500m,
                    MonthlyOperatingExpenses = 400m,
                    SecuredByLiabilityId = 9
                }
            }
        };

        var result = _calc.Project(plan, years: 0);

        Assert.Equal(0m, result.TotalAnnualRentalIncome);
    }

    [Fact]
    public void Project_NoRealEstateAccounts_ReturnsEmptySummary()
    {
        var result = _calc.Project(new FinancialPlan(), years: 5);

        Assert.Empty(result.Properties);
        Assert.Equal(0m, result.TotalProjectedEquity);
        Assert.Equal(0m, result.TotalAnnualRentalIncome);
    }
}
