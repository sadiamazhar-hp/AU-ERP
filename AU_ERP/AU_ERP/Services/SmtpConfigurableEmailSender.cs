using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AU_ERP.Services
{
    public class SmtpConfigurableEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpConfigurableEmailSender> _logger;

        public SmtpConfigurableEmailSender(IConfiguration configuration, ILogger<SmtpConfigurableEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var section = _configuration.GetSection("Email:Smtp");
            var host = section["Host"];

            var fromAddr = _configuration["Email:FromAddress"] ?? "noreply@localhost";
            var fromName = _configuration["Email:FromName"] ?? "AU MANUFACTURERS";

            if (string.IsNullOrWhiteSpace(host))
            {
                var preview = $"To: {toEmail}{Environment.NewLine}Subject: {subject}{Environment.NewLine}---{Environment.NewLine}{htmlBody}";
                _logger.LogWarning("Email:Smtp:Host is not configured. Logging message instead:{NewLine}{Preview}",
                    Environment.NewLine, preview);
                return;
            }

            using var msg = new MailMessage
            {
                From = new MailAddress(fromAddr, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(toEmail);

            var port = int.TryParse(section["Port"], out var p) ? p : 587;
            var enableSsl = bool.TryParse(section["EnableSsl"], out var ssl) ? ssl : true;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(section["User"]))
                client.Credentials = new NetworkCredential(section["User"], section["Password"]);

            await client.SendMailAsync(msg).ConfigureAwait(false);
        }
    }
}
