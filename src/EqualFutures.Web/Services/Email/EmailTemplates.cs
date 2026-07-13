using System.Net;

namespace EqualFutures.Web.Services.Email;

/// <summary>
/// Builds branded HTML email bodies. User-controlled values (emails, household
/// names) are HTML-encoded to prevent injection into the message markup.
/// </summary>
public static class EmailTemplates
{
    private const string Teal = "#1f6f6b";
    private const string Ink = "#1f2933";
    private const string Muted = "#6b7684";
    private const string Border = "#e4e8ee";

    public static (string Subject, string Html) Confirmation(string confirmationLink)
    {
        var html = Layout(
            "Confirm your email",
            $"""
            <p>Welcome to EqualFutures! Please confirm your email address to activate your account.</p>
            {Button("Confirm my email", confirmationLink)}
            <p style="color:{Muted};font-size:13px">If the button doesn't work, copy and paste this link into your browser:<br/>
            <a href="{HtmlAttr(confirmationLink)}" style="color:{Teal}">{Html(confirmationLink)}</a></p>
            """);
        return ("Confirm your EqualFutures email", html);
    }

    public static (string Subject, string Html) PasswordReset(string resetLink)
    {
        var html = Layout(
            "Reset your password",
            $"""
            <p>We received a request to reset your EqualFutures password. Click below to choose a new one.</p>
            {Button("Reset my password", resetLink)}
            <p style="color:{Muted};font-size:13px">If you didn't request this, you can safely ignore this email.</p>
            """);
        return ("Reset your EqualFutures password", html);
    }

    public static (string Subject, string Html) PasswordResetCode(string resetCode)
    {
        var html = Layout(
            "Reset your password",
            $"""
            <p>Use the following code to reset your EqualFutures password:</p>
            <p style="font-size:22px;font-weight:700;letter-spacing:2px;color:{Ink}">{Html(resetCode)}</p>
            """);
        return ("Your EqualFutures password reset code", html);
    }

    public static (string Subject, string Html) Invitation(string householdName, string inviterEmail, string role, string joinLink)
    {
        var safeHousehold = Html(householdName);
        var html = Layout(
            "You're invited to a family plan",
            $"""
            <p><strong>{Html(inviterEmail)}</strong> invited you to join the <strong>{safeHousehold}</strong>
            plan on EqualFutures as a <strong>{Html(role)}</strong>.</p>
            <p>EqualFutures helps families balance retirement security with their children's education — fairly.</p>
            {Button("Join the family plan", joinLink)}
            <p style="color:{Muted};font-size:13px">Sign in (or register) with this email address, then open the link above.
            If the button doesn't work, paste this into your browser:<br/>
            <a href="{HtmlAttr(joinLink)}" style="color:{Teal}">{Html(joinLink)}</a></p>
            """);
        return ($"You're invited to join {householdName} on EqualFutures", html);
    }

    private static string Layout(string heading, string bodyHtml) =>
        $"""
        <div style="font-family:'Segoe UI',Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:{Ink};line-height:1.5">
          <div style="font-size:22px;font-weight:700;color:{Teal};margin-bottom:8px">EqualFutures</div>
          <h2 style="font-size:18px;margin:0 0 12px">{Html(heading)}</h2>
          {bodyHtml}
          <hr style="border:none;border-top:1px solid {Border};margin:24px 0" />
          <p style="color:{Muted};font-size:12px;margin:0">EqualFutures · Plan Today. Support Dreams. Build Tomorrow.</p>
        </div>
        """;

    private static string Button(string text, string href) =>
        $"""
        <p style="margin:20px 0">
          <a href="{HtmlAttr(href)}" style="background:{Teal};color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:8px;display:inline-block;font-weight:600">{Html(text)}</a>
        </p>
        """;

    private static string Html(string value) => WebUtility.HtmlEncode(value ?? string.Empty);

    // Links from Identity/NavigationManager are already valid URLs; encode for safe attribute embedding.
    private static string HtmlAttr(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
