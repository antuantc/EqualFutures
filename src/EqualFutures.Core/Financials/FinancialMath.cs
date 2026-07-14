namespace EqualFutures.Core.Financials;

/// <summary>
/// Pure, side-effect-free financial math primitives. Every projection in the app
/// is built from these functions so the formulas can be unit tested in isolation.
/// All rates are per-period decimals (e.g. 0.07 == 7%).
/// </summary>
public static class FinancialMath
{
    /// <summary>Future value of a single present amount compounded annually.</summary>
    public static decimal FutureValue(decimal presentValue, decimal annualRate, int years)
    {
        if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
        return presentValue * Pow(1m + annualRate, years);
    }

    /// <summary>Present value of a single future amount discounted annually.</summary>
    public static decimal PresentValue(decimal futureValue, decimal annualRate, int years)
    {
        if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
        return futureValue / Pow(1m + annualRate, years);
    }

    /// <summary>
    /// Future value of a series of equal end-of-year contributions (ordinary annuity).
    /// </summary>
    public static decimal FutureValueOfAnnuity(decimal payment, decimal annualRate, int years)
    {
        if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
        if (years == 0) return 0m;
        if (annualRate == 0m) return payment * years;
        return payment * ((Pow(1m + annualRate, years) - 1m) / annualRate);
    }

    /// <summary>
    /// Projects a starting balance forward, adding a fixed annual contribution at the
    /// end of each year and compounding at <paramref name="annualRate"/>.
    /// </summary>
    public static decimal ProjectBalance(decimal startingBalance, decimal annualContribution, decimal annualRate, int years)
        => FutureValue(startingBalance, annualRate, years) + FutureValueOfAnnuity(annualContribution, annualRate, years);

    /// <summary>Grows a today's-dollars amount by inflation to a future year.</summary>
    public static decimal InflateValue(decimal todaysValue, decimal inflationRate, int years)
        => FutureValue(todaysValue, inflationRate, years);

    /// <summary>
    /// Present value of a stream of level real (inflation-adjusted) withdrawals lasting
    /// <paramref name="years"/> years, discounted at a real rate. Used to size the nest
    /// egg required to fund retirement spending.
    /// </summary>
    public static decimal PresentValueOfAnnuityDue(decimal annualWithdrawal, decimal realRate, int years)
    {
        if (years <= 0) return 0m;
        if (realRate == 0m) return annualWithdrawal * years;
        var ordinary = annualWithdrawal * ((1m - Pow(1m + realRate, -years)) / realRate);
        // Annuity due: first withdrawal happens immediately.
        return ordinary * (1m + realRate);
    }

    /// <summary>
    /// Real (inflation-adjusted) rate of return from a nominal rate, via the Fisher equation.
    /// </summary>
    public static decimal RealRate(decimal nominalRate, decimal inflationRate)
        => (1m + nominalRate) / (1m + inflationRate) - 1m;

    /// <summary>
    /// Remaining principal on an amortizing loan (e.g. a mortgage) after making
    /// <paramref name="monthlyPayment"/> for <paramref name="monthsElapsed"/> months.
    /// </summary>
    public static decimal AmortizedBalance(decimal principal, decimal annualRate, decimal monthlyPayment, int monthsElapsed)
    {
        if (monthsElapsed <= 0) return Math.Max(0m, principal);
        if (annualRate <= 0m) return Math.Max(0m, principal - monthlyPayment * monthsElapsed);

        decimal monthlyRate = annualRate / 12m;
        decimal growth = Pow(1m + monthlyRate, monthsElapsed);
        decimal balance = principal * growth - monthlyPayment * ((growth - 1m) / monthlyRate);
        return Math.Max(0m, balance);
    }

    /// <summary>
    /// Integer-exponent power for <see cref="decimal"/> to avoid double rounding drift in
    /// financial figures. Supports negative exponents.
    /// </summary>
    public static decimal Pow(decimal baseValue, int exponent)
    {
        if (exponent == 0) return 1m;
        if (exponent < 0) return 1m / Pow(baseValue, -exponent);

        decimal result = 1m;
        decimal factor = baseValue;
        int e = exponent;
        while (e > 0)
        {
            if ((e & 1) == 1) result *= factor;
            e >>= 1;
            if (e > 0) factor *= factor;
        }
        return result;
    }
}
