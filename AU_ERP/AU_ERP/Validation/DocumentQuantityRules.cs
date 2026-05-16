namespace AU_ERP.Validation;

/// <summary>
/// Operational document quantities: no negatives, no fractional amounts (whole numbers only).
/// </summary>
public static class DocumentQuantityRules
{
    public static bool IsWholeNonNegative(decimal q)
        => q >= 0 && q == decimal.Truncate(q);

    public static bool IsWholePositive(decimal q)
        => q > 0 && q == decimal.Truncate(q);

    public static string? ValidatePositiveWhole(decimal q, string fieldLabel = "Quantity")
        => IsWholePositive(q)
            ? null
            : $"{fieldLabel} must be a positive whole number (no decimals or negatives).";

    public static string? ValidateNonNegativeWhole(decimal q, string fieldLabel = "Quantity")
        => IsWholeNonNegative(q)
            ? null
            : $"{fieldLabel} must be a whole number with no decimals (negative values are not allowed).";
}
