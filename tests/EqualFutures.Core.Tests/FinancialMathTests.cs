using EqualFutures.Core.Financials;

namespace EqualFutures.Core.Tests;

public class FinancialMathTests
{
    [Fact]
    public void FutureValue_CompoundsAnnually()
    {
        Assert.Equal(1210m, FinancialMath.FutureValue(1000m, 0.10m, 2));
    }

    [Fact]
    public void PresentValue_IsInverseOfFutureValue()
    {
        var fv = FinancialMath.FutureValue(1000m, 0.07m, 10);
        var pv = FinancialMath.PresentValue(fv, 0.07m, 10);
        Assert.Equal(1000m, Math.Round(pv, 6));
    }

    [Fact]
    public void FutureValueOfAnnuity_SumsCompoundedPayments()
    {
        // 100 at 10% for 3 years: 100 * (1.331 - 1)/0.1 = 331
        Assert.Equal(331m, Math.Round(FinancialMath.FutureValueOfAnnuity(100m, 0.10m, 3), 6));
    }

    [Fact]
    public void FutureValueOfAnnuity_ZeroRate_IsPaymentTimesYears()
    {
        Assert.Equal(500m, FinancialMath.FutureValueOfAnnuity(100m, 0m, 5));
    }

    [Fact]
    public void ProjectBalance_CombinesGrowthAndContributions()
    {
        // FV(1000,10%,2)=1210 ; FVA(100,10%,2)=210 ; total 1420
        Assert.Equal(1420m, Math.Round(FinancialMath.ProjectBalance(1000m, 100m, 0.10m, 2), 6));
    }

    [Fact]
    public void Pow_SupportsNegativeExponents()
    {
        Assert.Equal(0.25m, FinancialMath.Pow(2m, -2));
    }

    [Fact]
    public void Pow_ZeroExponent_IsOne()
    {
        Assert.Equal(1m, FinancialMath.Pow(1.07m, 0));
    }

    [Fact]
    public void FutureValue_NegativeYears_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FinancialMath.FutureValue(1000m, 0.05m, -1));
    }

    [Fact]
    public void AmortizedBalance_ZeroMonthsElapsed_ReturnsPrincipal()
    {
        Assert.Equal(300_000m, FinancialMath.AmortizedBalance(300_000m, 0.06m, 1_798.65m, 0));
    }

    [Fact]
    public void AmortizedBalance_ZeroRate_SubtractsPaymentsLinearly()
    {
        Assert.Equal(2_000m, FinancialMath.AmortizedBalance(12_000m, 0m, 1_000m, 10));
    }

    [Fact]
    public void AmortizedBalance_FullTermPayoff_ReachesZero()
    {
        // Derive the standard level-payment amount for a 30-year loan and confirm the
        // balance formula is self-consistent: paying it for the full term pays it off.
        decimal principal = 300_000m;
        decimal monthlyRate = 0.06m / 12m;
        int totalMonths = 360;
        decimal growth = FinancialMath.Pow(1m + monthlyRate, totalMonths);
        decimal payment = principal * monthlyRate * growth / (growth - 1m);

        decimal balance = FinancialMath.AmortizedBalance(principal, 0.06m, payment, totalMonths);

        Assert.Equal(0m, Math.Round(balance, 2));
    }

    [Fact]
    public void AmortizedBalance_PartwayThroughTerm_DecreasesOverTime()
    {
        decimal principal = 300_000m;
        decimal payment = 1_798.65m; // approx standard payment for 300k / 30yr / 6%

        decimal early = FinancialMath.AmortizedBalance(principal, 0.06m, payment, 12);
        decimal later = FinancialMath.AmortizedBalance(principal, 0.06m, payment, 120);

        Assert.True(later < early);
        Assert.True(early < principal);
    }
}
