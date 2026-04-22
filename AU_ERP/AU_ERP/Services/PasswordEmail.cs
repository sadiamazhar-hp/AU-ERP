using AU_ERP.Models;

namespace AU_ERP.Services;

public static class PasswordEmail
{
    public static Task SendInvitationAsync(IEmailService emailService, IConfiguration configuration, ApplicationUser user, string token)
    {
        var link = AccountLinkBuilder.PasswordSetLink(configuration, user.Id!, token);
        var html =
            $"""
            <p>Hello,</p>
            <p>An account has been created for you on AU ERP. Please set your password using the secure link below.</p>
            <p><a href="{link}" style="display:inline-block;padding:12px 24px;background:#2563eb;color:#fff;text-decoration:none;border-radius:8px;font-family:sans-serif;">Set password</a></p>
            <p style="font-size:12px;color:#64748b;">If you did not expect this email, you can ignore it.</p>
            """;
        return emailService.SendHtmlEmailAsync(user.Email!, "Set your AU ERP password", html);
    }

    public static Task SendForgotPasswordAsync(IEmailService emailService, IConfiguration configuration, ApplicationUser user, string token)
    {
        var link = AccountLinkBuilder.PasswordSetLink(configuration, user.Id!, token);
        var html =
            $"""
            <p>Hello,</p>
            <p>We received a request to reset your AU ERP password. Use the link below to choose a new password.</p>
            <p><a href="{link}" style="display:inline-block;padding:12px 24px;background:#2563eb;color:#fff;text-decoration:none;border-radius:8px;font-family:sans-serif;">Reset password</a></p>
            <p style="font-size:12px;color:#64748b;">If you did not request this, you can ignore this email.</p>
            """;
        return emailService.SendHtmlEmailAsync(user.Email!, "Reset your AU ERP password", html);
    }
}
