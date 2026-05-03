namespace AU_ERP.Models;

/// <summary>Optional letterhead lines for PDFs from Company info (singleton).</summary>
public readonly struct CompanyPdfHeader
{
    public CompanyPdfHeader(string? companyName, string? phoneNumber)
    {
        CompanyName = string.IsNullOrWhiteSpace(companyName) ? null : companyName.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
    }

    public string? CompanyName { get; }
    public string? PhoneNumber { get; }
    public bool HasAny => CompanyName != null || PhoneNumber != null;
}
