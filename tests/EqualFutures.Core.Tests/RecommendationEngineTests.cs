using EqualFutures.Core.Education;
using EqualFutures.Core.Fairness;
using EqualFutures.Core.Recommendations;
using EqualFutures.Core.Retirement;
using EqualFutures.Domain;

namespace EqualFutures.Core.Tests;

public class RecommendationEngineTests
{
    private readonly RecommendationEngine _engine = new();
    private static readonly FairnessResult NeutralFairness = new();
    private static readonly RetirementProjection OnTrackRetirement = new() { FundingGap = 0m };

    private static EducationProjection NoGapProjection(int id, string name) => new()
    {
        ChildId = id,
        ChildName = name,
        FundingGap = 0m,
        ProjectedEducationSavings = 0m,
        DesiredFamilyContribution = 0m
    };

    [Fact]
    public void Generate_HsaWithNoContribution_RecommendsPrioritizingIt()
    {
        var plan = new FinancialPlan
        {
            Accounts = { new Account { Name = "HSA", Type = AccountType.Hsa, Category = AccountCategory.Investment, CurrentBalance = 5_000m, AnnualContribution = 0m } }
        };

        var recs = _engine.Generate(plan, OnTrackRetirement, Array.Empty<EducationProjection>(), NeutralFairness);

        Assert.Contains(recs, r => r.Category == RecommendationCategory.Allocation && r.Title.Contains("HSA"));
    }

    [Fact]
    public void Generate_HsaWithContribution_NoHsaRecommendation()
    {
        var plan = new FinancialPlan
        {
            Accounts = { new Account { Name = "HSA", Type = AccountType.Hsa, Category = AccountCategory.Investment, CurrentBalance = 5_000m, AnnualContribution = 3_500m } }
        };

        var recs = _engine.Generate(plan, OnTrackRetirement, Array.Empty<EducationProjection>(), NeutralFairness);

        Assert.DoesNotContain(recs, r => r.Title.Contains("HSA"));
    }

    [Fact]
    public void Generate_TaxableFundedWhileTaxAdvantagedIdle_RecommendsRedirecting()
    {
        var plan = new FinancialPlan
        {
            Accounts =
            {
                new Account { Name = "Brokerage", Type = AccountType.Brokerage, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.Taxable, CurrentBalance = 50_000m, AnnualContribution = 6_000m },
                new Account { Name = "Traditional IRA", Type = AccountType.TraditionalIra, Category = AccountCategory.Investment, TaxTreatment = TaxTreatment.TaxDeferred, CurrentBalance = 20_000m, AnnualContribution = 0m }
            }
        };

        var recs = _engine.Generate(plan, OnTrackRetirement, Array.Empty<EducationProjection>(), NeutralFairness);

        Assert.Contains(recs, r => r.Category == RecommendationCategory.Allocation && r.Title.Contains("tax-advantaged", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Generate_CashAboveEmergencyFund_RecommendsMovingExcess()
    {
        var plan = new FinancialPlan
        {
            ExpectedAnnualRetirementSpending = 60_000m, // 6-month target = 30,000
            Accounts = { new Account { Name = "Checking", Type = AccountType.BankAccount, Category = AccountCategory.Other, CurrentBalance = 50_000m } }
        };
        var shortfallRetirement = new RetirementProjection { FundingGap = -100_000m };

        var recs = _engine.Generate(plan, shortfallRetirement, Array.Empty<EducationProjection>(), NeutralFairness);

        var rec = Assert.Single(recs, r => r.Title == "Put idle cash to work");
        Assert.Contains("20,000", rec.Detail); // 50,000 - 30,000 excess
        Assert.Contains("retirement", rec.Reasoning);
    }

    [Fact]
    public void Generate_CashBelowEmergencyFund_NoIdleCashRecommendation()
    {
        var plan = new FinancialPlan
        {
            ExpectedAnnualRetirementSpending = 90_000m, // 6-month target = 45,000
            Accounts = { new Account { Name = "Checking", Type = AccountType.BankAccount, Category = AccountCategory.Other, CurrentBalance = 40_000m } }
        };

        var recs = _engine.Generate(plan, OnTrackRetirement, Array.Empty<EducationProjection>(), NeutralFairness);

        Assert.DoesNotContain(recs, r => r.Title == "Put idle cash to work");
    }

    [Fact]
    public void Generate_OverfundedEducationAccount_RecommendsRedirecting()
    {
        var plan = new FinancialPlan
        {
            Children = { new Child { Id = 1, Name = "Maya" } },
            Accounts = { new Account { Name = "529 - Maya", Category = AccountCategory.Education, Type = AccountType.FiveTwoNine, BeneficiaryChildId = 1, CurrentBalance = 100_000m } }
        };
        var education = new[]
        {
            new EducationProjection { ChildId = 1, ChildName = "Maya", DesiredFamilyContribution = 40_000m, ProjectedEducationSavings = 80_000m }
        };

        var recs = _engine.Generate(plan, OnTrackRetirement, education, NeutralFairness);

        Assert.Contains(recs, r => r.Category == RecommendationCategory.Allocation && r.Title.Contains("Maya"));
    }

    [Fact]
    public void Generate_EducationOnTarget_NoOverfundedRecommendation()
    {
        var plan = new FinancialPlan
        {
            Children = { new Child { Id = 1, Name = "Maya" } },
            Accounts = { new Account { Name = "529 - Maya", Category = AccountCategory.Education, Type = AccountType.FiveTwoNine, BeneficiaryChildId = 1, CurrentBalance = 40_000m } }
        };
        var education = new[] { NoGapProjection(1, "Maya") with { DesiredFamilyContribution = 40_000m, ProjectedEducationSavings = 42_000m } };

        var recs = _engine.Generate(plan, OnTrackRetirement, education, NeutralFairness);

        Assert.DoesNotContain(recs, r => r.Title.Contains("ahead of target"));
    }
}
