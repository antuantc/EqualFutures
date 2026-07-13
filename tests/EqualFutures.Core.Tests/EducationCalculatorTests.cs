using EqualFutures.Core.Education;
using EqualFutures.Domain;

namespace EqualFutures.Core.Tests;

public class EducationCalculatorTests
{
    private readonly EducationCalculator _calc = new();

    [Theory]
    [InlineData(CollegeType.PublicUniversity, 27_000)]
    [InlineData(CollegeType.PrivateUniversity, 58_000)]
    [InlineData(CollegeType.NoCollege, 0)]
    public void DefaultAnnualCost_MatchesTable(CollegeType type, decimal expected)
    {
        Assert.Equal(expected, _calc.DefaultAnnualCost(type));
    }

    [Fact]
    public void Project_UnfundedChild_ReportsFullGap()
    {
        int year = DateTime.UtcNow.Year;
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { EducationInflationRate = 0.05m }
        };
        var child = new Child
        {
            Id = 1,
            Name = "Sam",
            ExpectedCollegeStartYear = year,   // starts now => no inflation
            YearsOfCollege = 1,
            AnnualCostOverride = 10_000m,
            DesiredFundingPercent = 1.0m,
            ExpectedAnnualScholarships = 0m
        };
        plan.Children.Add(child);

        var e = _calc.Project(plan, child);

        Assert.Equal(10_000m, e.TotalCostFutureDollars);
        Assert.Equal(10_000m, e.DesiredFamilyContribution);
        Assert.Equal(0m, e.ProjectedEducationSavings);
        Assert.Equal(10_000m, e.FundingGap);
        Assert.Equal(0m, e.PercentFunded);
    }

    [Fact]
    public void Project_EarmarkedSavingsCloseTheGap()
    {
        int year = DateTime.UtcNow.Year;
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { EducationInflationRate = 0.05m, PreRetirementReturn = 0m }
        };
        var child = new Child
        {
            Id = 7,
            Name = "Mia",
            ExpectedCollegeStartYear = year,
            YearsOfCollege = 1,
            AnnualCostOverride = 10_000m,
            DesiredFundingPercent = 1.0m
        };
        plan.Children.Add(child);
        plan.Accounts.Add(new Account
        {
            Category = AccountCategory.Education,
            BeneficiaryChildId = 7,
            CurrentBalance = 10_000m,
            AnnualContribution = 0m
        });

        var e = _calc.Project(plan, child);

        Assert.Equal(10_000m, e.ProjectedEducationSavings);
        Assert.Equal(0m, e.FundingGap);
        Assert.Equal(100m, e.PercentFunded);
    }

    [Fact]
    public void Project_ScholarshipsReduceFamilyContribution()
    {
        int year = DateTime.UtcNow.Year;
        var plan = new FinancialPlan { Assumptions = new PlanAssumptions { EducationInflationRate = 0m } };
        var child = new Child
        {
            Id = 3,
            ExpectedCollegeStartYear = year,
            YearsOfCollege = 1,
            AnnualCostOverride = 10_000m,
            DesiredFundingPercent = 1.0m,
            ExpectedAnnualScholarships = 4_000m
        };
        plan.Children.Add(child);

        var e = _calc.Project(plan, child);

        Assert.Equal(4_000m, e.ExpectedScholarships);
        Assert.Equal(6_000m, e.DesiredFamilyContribution);   // (10k - 4k) * 100%
    }
}
