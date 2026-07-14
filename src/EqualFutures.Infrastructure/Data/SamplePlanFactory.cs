using EqualFutures.Domain;

namespace EqualFutures.Infrastructure.Data;

/// <summary>
/// Builds a realistic sample household on demand so users can explore a fully
/// populated plan. Seeding is opt-in — new accounts start empty and the user
/// chooses to load (or later clear) this sample. Education accounts are linked
/// to children by name after the plan is first saved (see <see cref="PlanService"/>).
/// </summary>
public static class SamplePlanFactory
{
    /// <summary>Builds a new sample plan owned by <paramref name="ownerId"/>.</summary>
    public static FinancialPlan Create(string ownerId)
    {
        var plan = new FinancialPlan { OwnerId = ownerId };
        Populate(plan);
        return plan;
    }

    /// <summary>
    /// Fills an existing (typically empty) plan with the sample household. The
    /// household name, assumptions, and fairness preference are overwritten.
    /// </summary>
    public static void Populate(FinancialPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        int thisYear = DateTime.UtcNow.Year;

        plan.HouseholdName = "The Rivera Family";
        plan.ExpectedAnnualRetirementSpending = 90_000m;
        plan.PreferredFairnessMetric = FairnessMetric.EqualInflationAdjustedValue;
        plan.Assumptions = new PlanAssumptions();

        plan.Parents.Add(new Parent
        {
            Name = "Jordan",
            BirthDate = new DateOnly(thisYear - 42, 1, 15),
            PlannedRetirementAge = 65,
            AnnualIncome = 120_000m,
            EstimatedAnnualSocialSecurity = 30_000m,
            SocialSecurityClaimingAge = 67,
            AnnualPensionIncome = 0m
        });
        plan.Parents.Add(new Parent
        {
            Name = "Alex",
            BirthDate = new DateOnly(thisYear - 40, 1, 15),
            PlannedRetirementAge = 65,
            AnnualIncome = 95_000m,
            EstimatedAnnualSocialSecurity = 26_000m,
            SocialSecurityClaimingAge = 67,
            AnnualPensionIncome = 0m
        });

        plan.Children.Add(new Child
        {
            Name = "Maya",
            BirthDate = new DateOnly(thisYear - 14, 5, 12),
            ExpectedCollegeStartYear = thisYear + 4,
            YearsOfCollege = 4,
            CollegeType = CollegeType.PublicUniversity,
            DesiredFundingPercent = 0.75m,
            ExpectedAnnualScholarships = 3_000m
        });
        plan.Children.Add(new Child
        {
            Name = "Leo",
            BirthDate = new DateOnly(thisYear - 11, 9, 3),
            ExpectedCollegeStartYear = thisYear + 7,
            YearsOfCollege = 4,
            CollegeType = CollegeType.PrivateUniversity,
            DesiredFundingPercent = 0.75m,
            ExpectedAnnualScholarships = 8_000m
        });

        plan.Accounts.Add(new Account { Name = "Jordan 401(k)", Type = AccountType.FourZeroOneK, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.TaxDeferred, CurrentBalance = 210_000m, AnnualContribution = 18_000m });
        plan.Accounts.Add(new Account { Name = "Alex 401(k)", Type = AccountType.FourZeroOneK, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.TaxDeferred, CurrentBalance = 145_000m, AnnualContribution = 14_000m });
        plan.Accounts.Add(new Account { Name = "Roth IRA", Type = AccountType.RothIra, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.TaxFree, CurrentBalance = 62_000m, AnnualContribution = 7_000m });
        plan.Accounts.Add(new Account { Name = "Brokerage", Type = AccountType.Brokerage, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.Taxable, CurrentBalance = 48_000m, AnnualContribution = 6_000m });
        plan.Accounts.Add(new Account { Name = "HSA", Type = AccountType.Hsa, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.TaxFree, CurrentBalance = 22_000m, AnnualContribution = 3_500m });
        plan.Accounts.Add(new Account { Name = "529 - Maya", Type = AccountType.FiveTwoNine, Category = AccountCategory.Education, TaxTreatment = TaxTreatment.TaxFree, CurrentBalance = 34_000m, AnnualContribution = 4_800m });
        plan.Accounts.Add(new Account { Name = "529 - Leo", Type = AccountType.FiveTwoNine, Category = AccountCategory.Education, TaxTreatment = TaxTreatment.TaxFree, CurrentBalance = 18_000m, AnnualContribution = 4_800m });
        plan.Accounts.Add(new Account { Name = "Checking & Savings", Type = AccountType.BankAccount, Category = AccountCategory.Other, TaxTreatment = TaxTreatment.Taxable, CurrentBalance = 40_000m, AnnualContribution = 0m });
        plan.Accounts.Add(new Account { Name = "Home", Type = AccountType.RealEstate, Category = AccountCategory.Other, TaxTreatment = TaxTreatment.Taxable, CurrentBalance = 520_000m, AnnualContribution = 0m });

        plan.Liabilities.Add(new Liability { Name = "Mortgage", Type = LiabilityType.Mortgage, CurrentBalance = 310_000m, InterestRate = 0.041m, MonthlyPayment = 2_150m });
        plan.Liabilities.Add(new Liability { Name = "Auto Loan", Type = LiabilityType.Other, CurrentBalance = 18_000m, InterestRate = 0.069m, MonthlyPayment = 430m });
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
