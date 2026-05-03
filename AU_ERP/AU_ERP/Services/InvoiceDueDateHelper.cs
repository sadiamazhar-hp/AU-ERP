using System.Globalization;
using System.Text.RegularExpressions;

namespace AU_ERP.Services;

public static class InvoiceDueDateHelper
{
    private const int DefaultDays = 30;

    /// <summary>Parses values like "10-days", "15-days", "30-days" from BP create form. Returns default 30 if unknown.</summary>
    public static int ResolvePaymentDays(string? paymentTerms)
    {
        var s = (paymentTerms ?? "").Trim();
        if (s.Length == 0)
            return DefaultDays;
        var m = Regex.Match(s, @"(\d+)\s*-\s*days", RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) && d > 0 && d <= 3650)
            return d;
        return DefaultDays;
    }

    public static DateTime ComputeDueDate(DateTime documentDate, string? paymentTerms)
    {
        var days = ResolvePaymentDays(paymentTerms);
        return documentDate.Date.AddDays(days);
    }
}
