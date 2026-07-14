namespace EqualFutures.Domain;

/// <summary>High level grouping used to organise accounts on the dashboard.</summary>
public enum AccountCategory
{
    Investment,
    Education,
    Other,

    /// <summary>Real property such as a primary home, rental, or land. Tracked for net worth
    /// but excluded from retirement/education projections since it isn't liquid savings.</summary>
    RealEstate
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
    Other,

    /// <summary>Loan on a motorcycle, boat, car, or truck.</summary>
    VehicleLoan,
    CreditCard,
    PersonalLoan
}

/// <summary>How a real estate account is used, which determines how it feeds retirement projections.</summary>
public enum RealEstateUse
{
    /// <summary>A home the household lives in. Appreciates, but its equity isn't liquid
    /// so it is excluded from retirement readiness by default.</summary>
    PrimaryResidence,

    /// <summary>An income-producing property. Appreciates like a primary residence, and its
    /// net rental cash flow (after vacancy, expenses, and debt service) counts as guaranteed
    /// retirement income.</summary>
    Rental
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

/// <summary>A member's level of access to a shared household plan.</summary>
public enum PlanRole
{
    /// <summary>Full control: edit everything, invite members, manage the family.</summary>
    Owner,

    /// <summary>An adult (e.g. spouse): can view and edit the plan, but not manage members.</summary>
    Adult,

    /// <summary>A child: read-only access to the plan.</summary>
    Child
}

/// <summary>Lifecycle state of a family invitation.</summary>
public enum InvitationStatus
{
    Pending,
    Accepted,
    Revoked
}
