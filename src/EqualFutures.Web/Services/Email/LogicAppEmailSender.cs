using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace EqualFutures.Web.Services.Email;

/// <summary>
/// Sends email by POSTing <c>{ "to", "subject", "body" }</c> to the Gmail-backed
/// Azure Logic App HTTP trigger. Failures are logged, not thrown, so a transient
/// email problem never breaks registration or invitations.
/// </summary>
public class LogicAppEmailSender(
    IHttpClientFactory httpClientFactory,
    IOptions<EmailOptions> options,
    ILogger<LogicAppEmailSender> logger) : IAppEmailSender
{
    public const string HttpClientName = "logicapp-email";

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var opts = options.Value;
        if (!opts.Enabled)
        {
            logger.LogWarning(
                "Email to {Recipient} was not sent because no Logic App URL is configured. " +
                "Set the 'Email:LogicAppUrl' secret to enable email.", to);
            return;
        }

        var payload = BuildPayload(opts, to, subject, htmlBody);
        var client = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            var json = JsonSerializer.Serialize(payload);

            // Send a clean "application/json" content type (no charset suffix) so the
            // Logic App HTTP trigger reliably parses the body into an object; otherwise
            // triggerBody()?['to'] can come back empty and the Gmail action fails.
            using var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await client.PostAsync(opts.LogicAppUrl, content, ct);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Email sent to {Recipient} (subject: {Subject}). Logic App status {Status}.",
                    to, subject, (int)response.StatusCode);
            }
            else
            {
                // Intentionally do not log the URL (it contains a SAS signature).
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError(
                    "Logic App returned {StatusCode} sending email to {Recipient}. Response: {Response}",
                    (int)response.StatusCode, to, responseBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Recipient} via the Logic App.", to);
        }
    }

    /// <summary>
    /// Builds the JSON payload. If explicit field names are configured, only those are
    /// sent. Otherwise a robust set of common recipient/subject/body key spellings is
    /// sent so the Gmail action finds a non-empty "To" regardless of how it's bound.
    /// </summary>
    private static Dictionary<string, string> BuildPayload(EmailOptions opts, string to, string subject, string htmlBody)
    {
        if (opts.HasExplicitFields)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [opts.ToField!] = to,
                [opts.SubjectField ?? "subject"] = subject,
                [opts.BodyField ?? "body"] = htmlBody
            };
        }

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["to"] = to,
            ["To"] = to,
            ["email"] = to,
            ["emailAddress"] = to,
            ["subject"] = subject,
            ["Subject"] = subject,
            ["body"] = htmlBody,
            ["Body"] = htmlBody,
            ["html"] = htmlBody,
            ["message"] = htmlBody
        };
    }
}
