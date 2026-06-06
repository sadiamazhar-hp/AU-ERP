using System.Text.RegularExpressions;

namespace AU_ERP.Validation;

/// <summary>Pakistan-standard contact validation for business partner communication fields.</summary>
public static class BusinessPartnerContactRules
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LandlineRegex = new(
        @"^\d{2,4}-\d{7,8}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MobileFormatRegex = new(
        @"^03\d{2}-\d{7}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string? ValidateEmail(string? email)
    {
        var v = (email ?? "").Trim();
        if (v.Length == 0)
            return null;
        if (!EmailRegex.IsMatch(v))
            return "Enter a valid email address (e.g. ahmed.khan@gmail.com).";
        return null;
    }

    public static string? ValidateTelephone(string? telephone)
    {
        var v = (telephone ?? "").Trim();
        if (v.Length == 0)
            return null;
        if (!LandlineRegex.IsMatch(v))
            return "Enter a valid Pakistan landline (e.g. 021-34567890 or 042-12345678). Use digits and one hyphen only.";
        return null;
    }

    public static string? ValidateMobile(string? mobile)
    {
        var v = (mobile ?? "").Trim();
        if (v.Length == 0)
            return null;
        if (!MobileFormatRegex.IsMatch(v))
            return "Enter a valid Pakistan mobile number (e.g. 0301-2345678). Format: 03XX-XXXXXXX.";
        var digits = Regex.Replace(v, @"\D", "");
        if (digits.Length != 11 || !digits.StartsWith("03", StringComparison.Ordinal))
            return "Mobile number must start with 03 and contain 11 digits (e.g. 0301-2345678).";
        return null;
    }
}
