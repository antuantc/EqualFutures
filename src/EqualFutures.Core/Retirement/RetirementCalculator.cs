using EqualFutures.Core.Financials;
using EqualFutures.Domain;

namespace EqualFutures.Core.Retirement;

/// <summary>
/// Deterministic retirement projection. Retirement assets grow with contributions
/// until the earliest parent's planned retirement, then must cover the portion of
/// household spending not met by Social Security and pensions.
/// </summary>
public class RetirementCalculator : IRetirementCalculator
{
    /// <summary>Account categories that count toward the retirement nest egg.</summary>
    private static bool IsRetirementAsset(Account a) =>
        a.Category == AccountCategory.Investment;

    public RetirementProjection Project(FinancialPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.Parents.Count == 0)
            return new RetirementProjection();

        var assumptions = plan.Assumptions;

        // The household begins drawing on the portfolio when the first parent retires.
        var primary = plan.Parents
            .OrderBy(p => p.PlannedRetirementAge - p.CurrentAge)
            .First();
        int years = Math.Max(0, primary.PlannedRetirementAge - primary.CurrentAge);

        // Grow each retirement account to the retirement date.
        var retirementAccounts = plan.Accounts.Where(IsRetirementAsset).ToList();
        decimal projectedNestEgg = retirementAccounts.Sum(a =>
            FinancialMath.ProjectBalance(
                a.CurrentBalance,
                a.AnnualContribution,
                a.ExpectedReturnOverride ?? assumptions.PreRetirementReturn,
                years));

        // Household spending need in future dollars.
        decimal todaysSpending = plan.ExpectedAnnualRetirementSpending;
        decimal annualSpendingNeed = FinancialMath.InflateValue(todaysSpending, assumptions.InflationRate, years);

        // Guaranteed income assumed to be inflation-indexed to the retirement date.
        decimal annualGuaranteedIncome = plan.Parents.Sum(p =>
            FinancialMath.InflateValue(
                p.EstimatedAnnualSocialSecurity + p.AnnualPensionIncome,
                assumptions.InflationRate,
                years));

        decimal annualPortfolioNeed = Math.Max(0m, annualSpendingNeed - annualGuaranteedIncome);

        // Nest egg required under the safe withdrawal rule.
        decimal requiredNestEgg = assumptions.SafeWithdrawalRate > 0m
            ? annualPortfolioNeed / assumptions.SafeWithdrawalRate
            : 0m;

        decimal safeWithdrawal = projectedNestEgg * assumptions.SafeWithdrawalRate;
        decimal fundingGap = projectedNestEgg - requiredNestEgg;

        decimal readiness = requiredNestEgg > 0m
            ? Math.Clamp(projectedNestEgg / requiredNestEgg * 100m, 0m, 100m)
            : 100m;

        return new RetirementProjection
        {
            YearsToRetirement = years,
            ProjectedNestEgg = projectedNestEgg,
            RequiredNestEgg = requiredNestEgg,
            FundingGap = fundingGap,
            SafeAnnualWithdrawal = safeWithdrawal,
            AnnualSpendingNeed = annualSpendingNeed,
            AnnualGuaranteedIncome = annualGuaranteedIncome,
            AnnualPortfolioNeed = annualPortfolioNeed,
            ReadinessScore = decimal.Round(readiness, 1),
            GrowthTrajectory = BuildTrajectory(retirementAccounts, assumptions, primary.CurrentAge, years)
        };
    }

    private static IReadOnlyList<BalancePoint> BuildTrajectory(
        List<Account> accounts, PlanAssumptions assumptions, int startAge, int years)
    {
        var points = new List<BalancePoint>(years + 1);
        int currentYear = DateTime.UtcNow.Year;
        for (int y = 0; y <= years; y++)
        {
            decimal balance = accounts.Sum(a =>
                FinancialMath.ProjectBalance(
                    a.CurrentBalance,
                    a.AnnualContribution,
                    a.ExpectedReturnOverride ?? assumptions.PreRetirementReturn,
                    y));
            points.Add(new BalancePoint(currentYear + y, startAge + y, decimal.Round(balance, 0)));
        }
        return points;
    }
}
