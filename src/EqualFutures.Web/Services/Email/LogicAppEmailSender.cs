using System.Net.Http.Json;
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

        var payload = new { to, subject, body = htmlBody };
        var client = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var response = await client.PostAsJsonAsync(opts.LogicAppUrl, payload, ct);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Email sent to {Recipient} (subject: {Subject}).", to, subject);
            }
            else
            {
                // Intentionally do not log the URL (it contains a SAS signature).
                logger.LogError(
                    "Logic App returned {StatusCode} sending email to {Recipient}.",
                    (int)response.StatusCode, to);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Recipient} via the Logic App.", to);
        }
    }
}
