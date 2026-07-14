using EqualFutures.Core.Retirement;
using EqualFutures.Domain;

namespace EqualFutures.Core.Tests;

public class PartnerEquityCalculatorTests
{
    private readonly PartnerEquityCalculator _calc = new();

    private static DateOnly BirthDateForAge(int age) => new(DateTime.UtcNow.Year - age, 1, 1);

    private static FinancialPlan PlanWithTwoParents(int? ownerParentIdA = null, int? ownerParentIdB = null)
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { PreRetirementReturn = 0.05m },
            Parents =
            {
                new Parent { Id = 1, Name = "Alex", BirthDate = BirthDateForAge(40), PlannedRetirementAge = 65 },
                new Parent { Id = 2, Name = "Sam", BirthDate = BirthDateForAge(40), PlannedRetirementAge = 65 }
            },
            Accounts =
            {
                new Account { Category = AccountCategory.Investment, CurrentBalance = 200_000m, AnnualContribution = 10_000m, OwnerParentId = 1 },
                new Account { Category = AccountCategory.Investment, CurrentBalance = 20_000m, AnnualContribution = 2_000m, OwnerParentId = 2 }
            }
        };
        return plan;
    }

    [Fact]
    public void Evaluate_NoParents_ReturnsEmptySnapshot()
    {
        var result = _calc.Evaluate(new FinancialPlan());
        Assert.False(result.HasComparablePair);
        Assert.Empty(result.Parents);
    }

    [Fact]
    public void Evaluate_TwoParents_IdentifiesLeadingAndLagging()
    {
        var plan = PlanWithTwoParents();

        var result = _calc.Evaluate(plan);

        Assert.True(result.HasComparablePair);
        Assert.Equal(1, result.LeadingParentId);
        Assert.Equal(2, result.LaggingParentId);
        Assert.True(result.ProjectedGap > 0m);
    }

    [Fact]
    public void Evaluate_JointAccountIsSplitEvenly()
    {
        var plan = new FinancialPlan
        {
            Assumptions = new PlanAssumptions { PreRetirementReturn = 0.05m },
            Parents =
            {
                new Parent { Id = 1, Name = "Alex", BirthDate = BirthDateForAge(40), PlannedRetirementAge = 65 },
                new Parent { Id = 2, Name = "Sam", BirthDate = BirthDateForAge(40), PlannedRetirementAge = 65 }
            },
            Accounts =
            {
                new Account { Category = AccountCategory.Investment, CurrentBalance = 100_000m, AnnualContribution = 0m, OwnerParentId = null }
            }
        };

        var result = _calc.Evaluate(plan);

        Assert.Equal(2, result.Parents.Count);
        Assert.All(result.Parents, p => Assert.Equal(50_000m, p.CurrentBalance));
        Assert.All(result.Parents, p => Assert.Equal(50m, p.SharePercent));
    }

    [Fact]
    public void ProjectCatchUp_TopUpNarrowsTheGapOverTime()
    {
        var plan = PlanWithTwoParents();
        var snapshot = _calc.Evaluate(plan);

        var noTopUp = _calc.ProjectCatchUp(plan, snapshot, monthlyTopUpAmount: 0m, careerBreakYears: 0);
        var withTopUp = _calc.ProjectCatchUp(plan, snapshot, monthlyTopUpAmount: 1_000m, careerBreakYears: 0);

        Assert.True(withTopUp.FinalGap < noTopUp.FinalGap);
    }

    [Fact]
    public void ProjectCatchUp_WithoutComparablePair_ReturnsEmptyProjection()
    {
        var plan = new FinancialPlan { Parents = { new Parent { Id = 1, Name = "Solo" } } };
        var snapshot = _calc.Evaluate(plan);

        var result = _calc.ProjectCatchUp(plan, snapshot, 500m, 2);

        Assert.Empty(result.Trajectory);
        Assert.Null(result.GapClosesYear);
    }
}
