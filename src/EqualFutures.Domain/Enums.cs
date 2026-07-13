namespace EqualFutures.Domain;

/// <summary>High level grouping used to organise accounts on the dashboard.</summary>
public enum AccountCategory
{
    Investment,
    Education,
    Other
}

/// <summary>Specific account type. Drives tax treatment and which module an account feeds.</summary>
public enum AccountType
{
    // Investment / retirement
    FourZeroOneK,
    TraditionalIra,
    RothIra,
    Brokerage,
    Hsa,

    // Education
    FiveTwoNine,
    UtmaUgma,
    EducationTrust,

    // Other assets
    BankAccount,
    RealEstate,
    Business
}

/// <summary>Tax treatment of an account, used by fairness and retirement calculations.</summary>
public enum TaxTreatment
{
    /// <summary>Contributions pre-tax, withdrawals taxed (401k, Traditional IRA).</summary>
    TaxDeferred,

    /// <summary>Contributions after-tax, qualified withdrawals tax-free (Roth, 529, HSA for eligible use).</summary>
    TaxFree,

    /// <summary>Growth taxed as realised (brokerage, bank, real estate).</summary>
    Taxable
}

public enum LiabilityType
{
    Mortgage,
    StudentLoan,
    Other
}

/// <summary>The kind of post-secondary path modelled for a child.</summary>
public enum CollegeType
{
    PublicUniversity,
    PrivateUniversity,
    TradeSchool,
    NoCollege,
    GraduateSchool
}

/// <summary>
/// The lens through which the fairness engine compares support across children.
/// Users may intentionally deviate from any of these.
/// </summary>
public enum FairnessMetric
{
    EqualDollarAmount,
    EqualInflationAdjustedValue,
    EqualPercentOfTuition,
    EqualLifetimeGifts,
    EqualAfterTaxBenefit
}
