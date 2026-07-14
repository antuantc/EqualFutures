using System.ComponentModel.DataAnnotations;

namespace EqualFutures.Domain;

/// <summary>A child whose education the household may help fund.</summary>
public class Child
{
    public int Id { get; set; }
    public int FinancialPlanId { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }

    /// <summary>Calendar year the child is expected to start college.</summary>
    public int ExpectedCollegeStartYear { get; set; }

    /// <summary>Number of years of schooling the household intends to help fund.</summary>
    public int YearsOfCollege { get; set; } = 4;

    /// <summary>Number of retirement groups associated with this child in the household plan.</summary>
    [Range(0, 10)]
    public int RetirementGroupCount { get; set; }

    /// <summary>The modelled college path, which drives the base annual cost.</summary>
    public CollegeType CollegeType { get; set; } = CollegeType.PublicUniversity;

    /// <summary>
    /// Optional override for annual college cost in today's dollars. When null the
    /// education calculator uses a default for the selected <see cref="CollegeType"/>.
    /// </summary>
    public decimal? AnnualCostOverride { get; set; }

    /// <summary>Share of total cost the family intends to cover, e.g. 0.75 for 75%.</summary>
    public decimal DesiredFundingPercent { get; set; } = 1.0m;

    /// <summary>Expected scholarships / grants per year (today's dollars).</summary>
    public decimal ExpectedAnnualScholarships { get; set; }

    /// <summary>Age used for fairness lifetime-gift comparisons and projections.</summary>
    public int AgeAsOf(DateOnly asOf)
    {
        var age = asOf.Year - BirthDate.Year;
        if (asOf < BirthDate.AddYears(age)) age--;
        return age;
    }
}
