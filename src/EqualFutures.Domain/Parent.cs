using System.ComponentModel.DataAnnotations;

namespace EqualFutures.Domain;

/// <summary>A parent / guardian in the household.</summary>
public class Parent
{
    public int Id { get; set; }
    public int FinancialPlanId { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public int CurrentAge { get; set; }
    public int PlannedRetirementAge { get; set; } = 65;

    /// <summary>Current gross annual employment income.</summary>
    public decimal AnnualIncome { get; set; }

    /// <summary>Estimated annual Social Security benefit at claiming age (today's dollars).</summary>
    public decimal EstimatedAnnualSocialSecurity { get; set; }

    /// <summary>Age at which Social Security is expected to be claimed.</summary>
    public int SocialSecurityClaimingAge { get; set; } = 67;

    /// <summary>Annual pension income, if any (today's dollars).</summary>
    public decimal AnnualPensionIncome { get; set; }
}
