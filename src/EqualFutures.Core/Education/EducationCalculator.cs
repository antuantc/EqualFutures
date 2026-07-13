using EqualFutures.Core.Financials;
using EqualFutures.Domain;

namespace EqualFutures.Core.Education;

/// <summary>
/// Estimates the inflation-adjusted cost of college for each child and compares it
/// against earmarked savings, scholarships, and the family's funding target.
/// </summary>
public class EducationCalculator : IEducationCalculator
{
    // Representative all-in annual costs (tuition, fees, room and board) in today's dollars.
    public decimal DefaultAnnualCost(CollegeType type) => type switch
    {
        CollegeType.PublicUniversity => 27_000m,
        CollegeType.PrivateUniversity => 58_000m,
        CollegeType.TradeSchool => 16_000m,
        CollegeType.GraduateSchool => 40_000m,
        CollegeType.NoCollege => 0m,
        _ => 0m
    };

    public EducationProjection Project(FinancialPlan plan, Child child)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(child);

        var assumptions = plan.Assumptions;
        int currentYear = DateTime.UtcNow.Year;
        int yearsUntilCollege = Math.Max(0, child.ExpectedCollegeStartYear - currentYear);
        int collegeYears = Math.Max(0, child.YearsOfCollege);

        decimal annualCostToday = child.AnnualCostOverride ?? DefaultAnnualCost(child.CollegeType);

        decimal totalCostToday = annualCostToday * collegeYears;
        decimal totalCostFuture = 0m;
        decimal scholarshipsFuture = 0m;
        for (int i = 0; i < collegeYears; i++)
        {
            int offset = yearsUntilCollege + i;
            totalCostFuture += FinancialMath.InflateValue(annualCostToday, assumptions.EducationInflationRate, offset);
            scholarshipsFuture += FinancialMath.InflateValue(child.ExpectedAnnualScholarships, assumptions.EducationInflationRate, offset);
        }

        // Grow education accounts earmarked to this child up to the start of college.
        decimal projectedSavings = plan.Accounts
            .Where(a => a.Category == AccountCategory.Education && a.BeneficiaryChildId == child.Id)
            .Sum(a => FinancialMath.ProjectBalance(
                a.CurrentBalance,
                a.AnnualContribution,
                a.ExpectedReturnOverride ?? assumptions.PreRetirementReturn,
                yearsUntilCollege));

        decimal netCostAfterScholarships = Math.Max(0m, totalCostFuture - scholarshipsFuture);
        decimal desiredFamilyContribution = netCostAfterScholarships * child.DesiredFundingPercent;
        decimal studentResponsibility = Math.Max(0m, netCostAfterScholarships - desiredFamilyContribution);

        decimal fundingGap = Math.Max(0m, desiredFamilyContribution - projectedSavings);
        decimal percentFunded = desiredFamilyContribution > 0m
            ? Math.Clamp(projectedSavings / desiredFamilyContribution * 100m, 0m, 100m)
            : 100m;

        return new EducationProjection
        {
            ChildId = child.Id,
            ChildName = child.Name,
            CollegeStartYear = child.ExpectedCollegeStartYear,
            YearsUntilCollege = yearsUntilCollege,
            TotalCostTodaysDollars = decimal.Round(totalCostToday, 0),
            TotalCostFutureDollars = decimal.Round(totalCostFuture, 0),
            ProjectedEducationSavings = decimal.Round(projectedSavings, 0),
            ExpectedScholarships = decimal.Round(scholarshipsFuture, 0),
            DesiredFamilyContribution = decimal.Round(desiredFamilyContribution, 0),
            StudentResponsibility = decimal.Round(studentResponsibility, 0),
            FundingGap = decimal.Round(fundingGap, 0),
            PercentFunded = decimal.Round(percentFunded, 1)
        };
    }

    public IReadOnlyList<EducationProjection> ProjectAll(FinancialPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan.Children.Select(c => Project(plan, c)).ToList();
    }
}
