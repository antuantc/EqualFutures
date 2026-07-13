namespace EqualFutures.Web.Services.Email;

/// <summary>Sends an HTML email through the configured delivery channel.</summary>
public interface IAppEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
