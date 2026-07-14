using EqualFutures.Core.Financials;
using EqualFutures.Domain;

namespace EqualFutures.Core.RealEstate;

/// <summary>
/// Projects real estate accounts forward. Every property appreciates at its own
/// <see cref="Account.ExpectedReturnOverride"/> or the plan's default
/// <see cref="PlanAssumptions.RealEstateAppreciationRate"/>. A property secured by a
/// mortgage nets that mortgage's projected (amortized) balance out of its value to get
/// equity. A property marked as a rental also projects net cash flow — rent, less
/// vacancy, operating expenses, a capital-expenditure reserve, and debt service — which
/// feeds the household's guaranteed retirement income.
/// </summary>
public class RealEstateCalculator : IRealEstateCalculator
{
    public RealEstateSummary Project(FinancialPlan plan, int years)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var properties = plan.Accounts
            .Where(a => a.Category == AccountCategory.RealEstate)
            .Select(a => ProjectOne(a, plan, years))
            .ToList();

        return new RealEstateSummary
        {
            Properties = properties,
            TotalProjectedEquity = properties.Sum(p => p.ProjectedEquity),
            TotalAnnualRentalIncome = properties.Sum(p => p.AnnualNetCashFlow)
        };
    }

    private static RealEstateProjection ProjectOne(Account account, FinancialPlan plan, int years)
    {
        var assumptions = plan.Assumptions;

        decimal appreciationRate = account.ExpectedReturnOverride ?? assumptions.RealEstateAppreciationRate;
        decimal projectedValue = FinancialMath.FutureValue(account.CurrentBalance, appreciationRate, years);

        var mortgage = account.SecuredByLiabilityId is int liabilityId
            ? plan.Liabilities.FirstOrDefault(l => l.Id == liabilityId)
            : null;

        decimal projectedMortgageBalance = mortgage is not null
            ? FinancialMath.AmortizedBalance(mortgage.CurrentBalance, mortgage.InterestRate, mortgage.MonthlyPayment, years * 12)
            : 0m;

        decimal projectedEquity = Math.Max(0m, projectedValue - projectedMortgageBalance);

        decimal annualNetCashFlow = 0m;
        if (account.Use == RealEstateUse.Rental && account.MonthlyRentToday is decimal rentToday && rentToday > 0m)
        {
            decimal futureMonthlyRent = FinancialMath.InflateValue(rentToday, assumptions.InflationRate, years);
            decimal grossAnnualRent = futureMonthlyRent * 12m * (1m - Math.Clamp(account.VacancyRate, 0m, 1m));

            decimal futureMonthlyExpenses = FinancialMath.InflateValue(account.MonthlyOperatingExpenses, assumptions.InflationRate, years);
            decimal annualExpenses = futureMonthlyExpenses * 12m;

            decimal capExReserve = projectedValue * account.AnnualCapExReservePercent;

            // Mortgage payments are fixed in nominal dollars for the life of a standard
            // fixed-rate loan, so debt service isn't inflated — only charged while owed.
            decimal annualDebtService = projectedMortgageBalance > 0m && mortgage is not null
                ? mortgage.MonthlyPayment * 12m
                : 0m;

            annualNetCashFlow = Math.Max(0m, grossAnnualRent - annualExpenses - capExReserve - annualDebtService);
        }

        return new RealEstateProjection
        {
            AccountId = account.Id,
            AccountName = account.Name,
            Use = account.Use ?? RealEstateUse.PrimaryResidence,
            ProjectedValue = decimal.Round(projectedValue, 0),
            ProjectedMortgageBalance = decimal.Round(projectedMortgageBalance, 0),
            ProjectedEquity = decimal.Round(projectedEquity, 0),
            AnnualNetCashFlow = decimal.Round(annualNetCashFlow, 0)
        };
    }
}
