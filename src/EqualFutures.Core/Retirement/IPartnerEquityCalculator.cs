using EqualFutures.Domain;

namespace EqualFutures.Core.Retirement;

/// <summary>
/// Compares retirement-asset trajectories between parents and models fairness
/// catch-up contributions, e.g. after a childcare career break.
/// </summary>
public interface IPartnerEquityCalculator
{
    /// <summary>Builds the household equity snapshot: each parent's projected balance and the leading/lagging gap.</summary>
    PartnerEquitySnapshot Evaluate(FinancialPlan plan);

    /// <summary>
    /// Projects the effect of redirecting a monthly amount from the leading parent to the
    /// lagging parent, optionally simulating a career-break period with reduced
    /// contributions for the lagging parent.
    /// </summary>
    CatchUpProjection ProjectCatchUp(FinancialPlan plan, PartnerEquitySnapshot snapshot, decimal monthlyTopUpAmount, int careerBreakYears);
}
