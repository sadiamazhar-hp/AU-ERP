namespace AU_ERP.Services;

public interface IEmailService
{
    Task SendHtmlEmailAsync(string toEmail, string subject, string htmlBody);
}
