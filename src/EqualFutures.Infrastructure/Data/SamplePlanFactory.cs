using EqualFutures.Domain;

namespace EqualFutures.Infrastructure.Data;

/// <summary>
/// Produces a realistic sample household so a brand-new user sees a populated
/// dashboard immediately instead of empty screens. Education accounts are linked
/// to children by name after the plan is first saved (see <see cref="PlanService"/>).
/// </summary>
public static class SamplePlanFactory
{
    public static FinancialPlan Create(string ownerId)
    {
        int thisYear = DateTime.UtcNow.Year;

        return new FinancialPlan
        {
            OwnerId = ownerId,
            HouseholdName = "The Rivera Family",
            PreferredFairnessMetric = FairnessMetric.EqualInflationAdjustedValue,
            Assumptions = new PlanAssumptions(),
            Parents =
            {
                new Parent
                {
                    Name = "Jordan",
                    CurrentAge = 42,
                    PlannedRetirementAge = 65,
                    AnnualIncome = 120_000m,
                    ExpectedAnnualRetirementSpending = 90_000m,
                    EstimatedAnnualSocialSecurity = 30_000m,
                    SocialSecurityClaimingAge = 67,
                    AnnualPensionIncome = 0m
                },
                new Parent
                {
                    Name = "Alex",
                    CurrentAge = 40,
                    PlannedRetirementAge = 65,
                    AnnualIncome = 95_000m,
                    ExpectedAnnualRetirementSpending = 90_000m,
                    EstimatedAnnualSocialSecurity = 26_000m,
                    SocialSecurityClaimingAge = 67,
                    AnnualPensionIncome = 0m
                }
            },
            Children =
            {
                new Child
                {
                    Name = "Maya",
                    BirthDate = new DateOnly(thisYear - 14, 5, 12),
                    ExpectedCollegeStartYear = thisYear + 4,
                    YearsOfCollege = 4,
                    CollegeType = CollegeType.PublicUniversity,
                    DesiredFundingPercent = 0.75m,
                    ExpectedAnnualScholarships = 3_000m
                },
                new Child
                {
                    Name = "Leo",
                    BirthDate = new DateOnly(thisYear - 11, 9, 3),
                    ExpectedCollegeStartYear = thisYear + 7,
                    YearsOfCollege = 4,
                    CollegeType = CollegeType.PrivateUniversity,
                    DesiredFundingPercent = 0.75m,
                    ExpectedAnnualScholarships = 8_000m
                }
            },
            Accounts =
            {
                new Account { Name = "Jordan 401(k)", Type = AccountType.FourZeroOneK, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.TaxDeferred, CurrentBalance = 210_000m, AnnualContribution = 18_000m },
                new Account { Name = "Alex 401(k)", Type = AccountType.FourZeroOneK, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.TaxDeferred, CurrentBalance = 145_000m, AnnualContribution = 14_000m },
                new Account { Name = "Roth IRA", Type = AccountType.RothIra, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.TaxFree, CurrentBalance = 62_000m, AnnualContribution = 7_000m },
                new Account { Name = "Brokerage", Type = AccountType.Brokerage, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.Taxable, CurrentBalance = 48_000m, AnnualContribution = 6_000m },
                new Account { Name = "HSA", Type = AccountType.Hsa, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.TaxFree, CurrentBalance = 22_000m, AnnualContribution = 3_500m },
                new Account { Name = "529 - Maya", Type = AccountType.FiveTwoNine, Category = AccountCategory.Education, TaxTreatment = TaxTreatment.TaxFree, CurrentBalance = 34_000m, AnnualContribution = 4_800m },
                new Account { Name = "529 - Leo", Type = AccountType.FiveTwoNine, Category = AccountCategory.Education, TaxTreatment = TaxTreatment.TaxFree, CurrentBalance = 18_000m, AnnualContribution = 4_800m },
                new Account { Name = "Checking & Savings", Type = AccountType.BankAccount, Category = AccountCategory.Other, TaxTreatment = TaxTreatment.Taxable, CurrentBalance = 40_000m, AnnualContribution = 0m },
                new Account { Name = "Home", Type = AccountType.RealEstate, Category = AccountCategory.Other, TaxTreatment = TaxTreatment.Taxable, CurrentBalance = 520_000m, AnnualContribution = 0m }
            },
            Liabilities =
            {
                new Liability { Name = "Mortgage", Type = LiabilityType.Mortgage, CurrentBalance = 310_000m, InterestRate = 0.041m, MonthlyPayment = 2_150m },
                new Liability { Name = "Auto Loan", Type = LiabilityType.Other, CurrentBalance = 18_000m, InterestRate = 0.069m, MonthlyPayment = 430m }
            }
        };
    }

    /// <summary>Links each education account to a child by matching the child's name in the account name.</summary>
    public static bool LinkEducationBeneficiaries(FinancialPlan plan)
    {
        bool changed = false;
        foreach (var account in plan.Accounts.Where(a => a.Category == AccountCategory.Education && a.BeneficiaryChildId is null))
        {
            var match = plan.Children.FirstOrDefault(c =>
                account.Name.Contains(c.Name, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                account.BeneficiaryChildId = match.Id;
                changed = true;
            }
        }
        return changed;
    }
}
