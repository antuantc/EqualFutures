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

    /// <summary>Emails are only sent when a Logic App URL is configured.</summary>
    public bool Enabled => !string.IsNullOrWhiteSpace(LogicAppUrl);
}
