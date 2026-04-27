using System.Globalization;

namespace AU_ERP;

/// <summary>Consistent 2-decimal display for money, quantities, and other UI decimals.</summary>
public static class UiNumberFormat
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string D2(decimal value) => value.ToString("N2", Inv);

    public static string D2(int value) => D2((decimal)value);
}
