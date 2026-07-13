using EqualFutures.Web.Data;
using Microsoft.AspNetCore.Identity;

namespace EqualFutures.Web.Services.Email;

/// <summary>
/// Bridges ASP.NET Core Identity's email hooks (confirmation, password reset) to the
/// Logic App sender using branded HTML templates.
/// </summary>
public class IdentityEmailSender(IAppEmailSender sender) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var (subject, html) = EmailTemplates.Confirmation(confirmationLink);
        return sender.SendAsync(email, subject, html);
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var (subject, html) = EmailTemplates.PasswordReset(resetLink);
        return sender.SendAsync(email, subject, html);
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var (subject, html) = EmailTemplates.PasswordResetCode(resetCode);
        return sender.SendAsync(email, subject, html);
    }
}
