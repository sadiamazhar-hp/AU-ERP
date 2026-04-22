using System.Net;
using System.Net.Mail;

namespace AU_ERP.Services;

public sealed class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendHtmlEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _configuration["Email:SmtpHost"];
        var from = _configuration["Email:FromEmail"] ?? "noreply@localhost";

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning(
                "Email not configured (Email:SmtpHost empty). Would send to {To}: {Subject}\n{Body}",
                toEmail, subject, htmlBody);
            return;
        }

        var port = _configuration.GetValue("Email:SmtpPort", 587);
        var user = _configuration["Email:SmtpUser"];
        var password = _configuration["Email:SmtpPassword"];
        var enableSsl = _configuration.GetValue("Email:SmtpUseSsl", true);

        using var message = new MailMessage
        {
            From = new MailAddress(from, _configuration["Email:FromName"] ?? "AU ERP"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            UseDefaultCredentials = string.IsNullOrEmpty(user),
            Credentials = string.IsNullOrEmpty(user)
                ? null
                : new NetworkCredential(user, password)
        };

        await client.SendMailAsync(message).ConfigureAwait(false);
    }
}
