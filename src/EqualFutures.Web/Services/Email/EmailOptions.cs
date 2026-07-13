namespace EqualFutures.Web.Services.Email;

/// <summary>Configuration for outbound email via the Gmail-backed Azure Logic App.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// The Logic App HTTP trigger URL (contains a SAS signature — treat as a secret,
    /// store in user-secrets / environment, never commit).
    /// </summary>
    public string LogicAppUrl { get; set; } = string.Empty;

    /// <summary>Display name used in email copy.</summary>
    public string FromDisplayName { get; set; } = "EqualFutures";

    /// <summary>
    /// Optional exact JSON property name for the recipient expected by the Logic App
    /// trigger / Gmail action. When set (along with the others), only these keys are
    /// sent. When left blank, a robust set of common aliases is sent instead.
    /// </summary>
    public string? ToField { get; set; }
    public string? SubjectField { get; set; }
    public string? BodyField { get; set; }

    /// <summary>Emails are only sent when a Logic App URL is configured.</summary>
    public bool Enabled => !string.IsNullOrWhiteSpace(LogicAppUrl);

    /// <summary>True when explicit field names have been provided.</summary>
    public bool HasExplicitFields => !string.IsNullOrWhiteSpace(ToField);
}
