using System.Text.RegularExpressions;

namespace AU_ERP.Services;

/// <summary>Normalization and validation for driver / fleet master fields (Pakistan-centric formats).</summary>
public static class FleetDriverInputNormalizer
{
    /// <summary>#####-#######-* after normalization (13 digits).</summary>
    public static bool TryNormalizePakCnic(string? raw, out string normalized, out string errorMessage)
    {
        normalized = "";
        errorMessage = "";
        var digits = Regex.Replace(raw ?? "", @"\D", "");
        if (digits.Length != 13)
        {
            errorMessage = "CNIC must be exactly 13 digits (format #####-#######-*).";
            return false;
        }

        normalized = $"{digits[..5]}-{digits.Substring(5, 7)}-{digits[12]}";
        return true;
    }

    /// <summary>Accepts "+92343…" / "92343…" / local 343… (10 digits from 3).</summary>
    public static bool TryNormalizePakMobile(string? raw, out string e164Plus, out string errorMessage)
    {
        e164Plus = "";
        errorMessage = "";
        var d = Regex.Replace(raw ?? "", @"\D", "");
        if (d.Length == 12 && d.StartsWith("92", StringComparison.Ordinal) && d[2] == '3')
        {
            e164Plus = "+" + d;
            return true;
        }

        if (d.Length == 10 && d[0] == '3')
        {
            e164Plus = "+92" + d;
            return true;
        }

        errorMessage = "Use 10 digits after +92 starting with 3 (e.g. 343-4256344).";
        return false;
    }

    /// <summary>Rejects values that read as a bare negative number; allows typical licence alphanumeric.</summary>
    public static bool IsValidDriverLicenceFormat(string? value, out string errorMessage)
    {
        errorMessage = "";
        var t = (value ?? "").Trim();
        if (t.StartsWith('-'))
        {
            errorMessage = "Licence number cannot start with '-' or be a negative number.";
            return false;
        }

        if (Regex.IsMatch(t, @"^-\d+$"))
        {
            errorMessage = "Licence number cannot be negative.";
            return false;
        }

        if (t.Length < 3)
        {
            errorMessage = "Licence number is too short.";
            return false;
        }

        if (!Regex.IsMatch(t, @"^[A-Za-z0-9][A-Za-z0-9\s\-/]*$"))
        {
            errorMessage = "Use letters, digits, spaces, hyphens, or slashes only.";
            return false;
        }

        return true;
    }

    public static bool IsValidVehicleNumberPlate(string? value, out string errorMessage)
    {
        errorMessage = "";
        var t = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(t))
            return true;

        if (t.StartsWith('-') || Regex.IsMatch(t, @"^-\d+$"))
        {
            errorMessage = "Number plate cannot start with '-' or be a negative number.";
            return false;
        }

        if (!Regex.IsMatch(t, @"^[A-Za-z0-9\s\-]+$"))
        {
            errorMessage = "Use letters, digits, spaces, or hyphens only.";
            return false;
        }

        return true;
    }
}
