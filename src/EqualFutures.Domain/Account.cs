using System.ComponentModel.DataAnnotations;

namespace EqualFutures.Domain;

/// <summary>
/// Any account holding value: retirement, education, or other assets. Accounts
/// may be earmarked to a specific child via <see cref="BeneficiaryChildId"/>.
/// </summary>
public class Account
{
    public int Id { get; set; }
    public int FinancialPlanId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public AccountCategory Category { get; set; }

    public TaxTreatment TaxTreatment { get; set; }

    /// <summary>Current market value.</summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>Planned annual contribution (today's dollars).</summary>
    public decimal AnnualContribution { get; set; }

    /// <summary>
    /// Optional override for expected annual return. When null the plan-level
    /// assumption is used.
    /// </summary>
    public decimal? ExpectedReturnOverride { get; set; }

    /// <summary>For child-specific education or retirement accounts, the child this balance is earmarked for.</summary>
    public int? BeneficiaryChildId { get; set; }

    /// <summary>
    /// For investment accounts, the parent this balance is attributed to. Used for
    /// retirement-equity comparisons between parents. Null means jointly owned/shared
    /// evenly across parents.
    /// </summary>
    public int? OwnerParentId { get; set; }
}
