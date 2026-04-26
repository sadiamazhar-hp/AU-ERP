namespace AU_ERP.Services;

public interface IEmailService
{
    Task SendHtmlEmailAsync(string toEmail, string subject, string htmlBody);

    /// <summary>Send HTML email with an optional file attachment (e.g. PDF).</summary>
    Task SendHtmlEmailWithAttachmentAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string attachmentFileName,
        byte[] attachmentBytes,
        string attachmentMediaType = "application/pdf");
}
