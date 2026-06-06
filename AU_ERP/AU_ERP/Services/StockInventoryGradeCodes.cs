namespace AU_ERP.Services;

/// <summary>Maps goods-receipt quality buckets to inventory grade keys.</summary>
public static class StockInventoryGradeCodes
{
    public const string FirstQuality = "A";
    public const string SecondQuality = "B";
    public const string ThirdQuality = "C";
    public const string Scrap = "Scrap";
    public const string None = "";

    /// <summary>Collapses grade aliases (A, Grade B, etc.) to canonical inventory keys.</summary>
    public static string NormalizeGradeKey(string? grade)
    {
        var g = (grade ?? "").Trim();
        if (g.Length == 0)
            return None;

        if (g.Equals(FirstQuality, StringComparison.OrdinalIgnoreCase)
            || g.Equals("Grade A", StringComparison.OrdinalIgnoreCase)
            || g.Equals("First Quality", StringComparison.OrdinalIgnoreCase))
            return FirstQuality;

        if (g.Equals(SecondQuality, StringComparison.OrdinalIgnoreCase)
            || g.Equals("Grade B", StringComparison.OrdinalIgnoreCase)
            || g.Equals("Second Quality", StringComparison.OrdinalIgnoreCase))
            return SecondQuality;

        if (g.Equals(ThirdQuality, StringComparison.OrdinalIgnoreCase)
            || g.Equals("Grade C", StringComparison.OrdinalIgnoreCase)
            || g.Equals("Third Quality", StringComparison.OrdinalIgnoreCase))
            return ThirdQuality;

        if (g.Equals(Scrap, StringComparison.OrdinalIgnoreCase)
            || g.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            return Scrap;

        return g.Length > 32 ? g[..32] : g;
    }

    public static string DisplayGradeLabel(string? gradeKey)
    {
        var key = NormalizeGradeKey(gradeKey);
        if (key == FirstQuality) return "Grade A";
        if (key == SecondQuality) return "Grade B";
        if (key == ThirdQuality) return "Grade C";
        if (key == Scrap) return "Scrap";
        return string.IsNullOrEmpty(key) ? "—" : key;
    }
}
