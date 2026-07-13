using System.ComponentModel.DataAnnotations;

namespace EqualFutures.Domain;

/// <summary>A debt owed by the household.</summary>
public class Liability
{
    public int Id { get; set; }
    public int FinancialPlanId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public LiabilityType Type { get; set; }

    /// <summary>Outstanding principal balance.</summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>Annual interest rate, e.g. 0.055 for 5.5%.</summary>
    public decimal InterestRate { get; set; }

    /// <summary>Scheduled monthly payment.</summary>
    public decimal MonthlyPayment { get; set; }
}
