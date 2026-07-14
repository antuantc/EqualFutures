using EqualFutures.Core.Financials;
using EqualFutures.Domain;

namespace EqualFutures.Core.Retirement;

/// <summary>
/// Compares each parent's projected retirement balance and models the impact of a
/// "fairness contribution" — a monthly transfer from the leading earner to the parent
/// who has fallen behind, often due to a childcare career break. Jointly-owned
/// investment accounts (no <see cref="Account.OwnerParentId"/>) are split evenly
/// across parents so every account is always attributed to someone.
/// </summary>
public class PartnerEquityCalculator : IPartnerEquityCalculator
{
    private static bool IsRetirementAsset(Account a) => a.Category == AccountCategory.Investment;

    public PartnerEquitySnapshot Evaluate(FinancialPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.Parents.Count == 0)
            return new PartnerEquitySnapshot();

        var assumptions = plan.Assumptions;
        var investmentAccounts = plan.Accounts.Where(IsRetirementAsset).ToList();
        var jointAccounts = investmentAccounts.Where(a => a.OwnerParentId is null).ToList();
        int parentCount = plan.Parents.Count;

        decimal jointBalanceShare = jointAccounts.Sum(a => a.CurrentBalance) / parentCount;
        decimal jointContributionShare = jointAccounts.Sum(a => a.AnnualContribution) / parentCount;

        var lines = plan.Parents.Select(p =>
        {
            var owned = investmentAccounts.Where(a => a.OwnerParentId == p.Id).ToList();
            decimal balance = owned.Sum(a => a.CurrentBalance) + jointBalanceShare;
            decimal contribution = owned.Sum(a => a.AnnualContribution) + jointContributionShare;
            int years = Math.Max(0, p.PlannedRetirementAge - p.CurrentAge);
            decimal projected = FinancialMath.ProjectBalance(balance, contribution, assumptions.PreRetirementReturn, years);

            return new PartnerEquityLine
            {
                ParentId = p.Id,
                ParentName = p.Name,
                CurrentAge = p.CurrentAge,
                PlannedRetirementAge = p.PlannedRetirementAge,
                CurrentBalance = decimal.Round(balance, 0),
                AnnualContribution = decimal.Round(contribution, 0),
                ProjectedBalance = decimal.Round(projected, 0)
            };
        }).ToList();

        decimal combined = lines.Sum(l => l.ProjectedBalance);
        var withShare = lines
            .Select(l => l with { SharePercent = combined > 0m ? decimal.Round(l.ProjectedBalance / combined * 100m, 1) : 0m })
            .ToList();

        int? leadingId = null;
        int? laggingId = null;
        decimal gap = 0m;

        if (withShare.Count >= 2)
        {
            var leading = withShare.OrderByDescending(l => l.ProjectedBalance).First();
            var lagging = withShare.OrderBy(l => l.ProjectedBalance).First();
            leadingId = leading.ParentId;
            laggingId = lagging.ParentId;
            gap = Math.Max(0m, leading.ProjectedBalance - lagging.ProjectedBalance);
        }

        return new PartnerEquitySnapshot
        {
            Parents = withShare,
            HasComparablePair = withShare.Count == 2,
            LeadingParentId = leadingId,
            LaggingParentId = laggingId,
            ProjectedGap = gap,
            CombinedProjectedBalance = decimal.Round(combined, 0)
        };
    }

    public CatchUpProjection ProjectCatchUp(FinancialPlan plan, PartnerEquitySnapshot snapshot, decimal monthlyTopUpAmount, int careerBreakYears)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!snapshot.HasComparablePair || snapshot.LeadingParentId is null || snapshot.LaggingParentId is null)
            return new CatchUpProjection();

        var leadingLine = snapshot.Parents.First(l => l.ParentId == snapshot.LeadingParentId);
        var laggingLine = snapshot.Parents.First(l => l.ParentId == snapshot.LaggingParentId);

        decimal rate = plan.Assumptions.PreRetirementReturn;
        decimal annualTopUp = Math.Max(0m, monthlyTopUpAmount) * 12m;
        int breakYears = Math.Max(0, careerBreakYears);

        int leadingYears = Math.Max(0, leadingLine.PlannedRetirementAge - leadingLine.CurrentAge);
        int laggingYears = Math.Max(0, laggingLine.PlannedRetirementAge - laggingLine.CurrentAge);
        int horizon = Math.Max(leadingYears, laggingYears);

        decimal leadingBalance = leadingLine.CurrentBalance;
        decimal laggingBalance = laggingLine.CurrentBalance;
        int currentYear = DateTime.UtcNow.Year;

        var points = new List<CatchUpYearPoint>(horizon + 1)
        {
            new(currentYear, decimal.Round(leadingBalance, 0), decimal.Round(laggingBalance, 0))
        };

        int? gapClosesYear = laggingBalance >= leadingBalance ? currentYear : null;

        // Direct year-by-year compounding (rather than a closed-form annuity formula)
        // because the annual contribution for each parent can change: the leading
        // parent's contribution is reduced by the top-up, and the lagging parent's
        // own contribution pauses during a career break.
        for (int y = 1; y <= horizon; y++)
        {
            decimal leadingContribution = y <= leadingYears
                ? Math.Max(0m, leadingLine.AnnualContribution - annualTopUp)
                : 0m;
            decimal laggingContribution = y > laggingYears
                ? 0m
                : annualTopUp + (y <= breakYears ? 0m : laggingLine.AnnualContribution);

            leadingBalance = leadingBalance * (1m + rate) + leadingContribution;
            laggingBalance = laggingBalance * (1m + rate) + laggingContribution;

            points.Add(new CatchUpYearPoint(currentYear + y, decimal.Round(leadingBalance, 0), decimal.Round(laggingBalance, 0)));

            if (gapClosesYear is null && laggingBalance >= leadingBalance)
                gapClosesYear = currentYear + y;
        }

        return new CatchUpProjection
        {
            LeadingParentId = leadingLine.ParentId,
            LaggingParentId = laggingLine.ParentId,
            MonthlyTopUpAmount = monthlyTopUpAmount,
            CareerBreakYears = breakYears,
            Trajectory = points,
            GapClosesYear = gapClosesYear,
            FinalGap = decimal.Round(Math.Max(0m, leadingBalance - laggingBalance), 0)
        };
    }
}
