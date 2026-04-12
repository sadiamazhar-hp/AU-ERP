namespace AU_ERP.Models
{
    /// <summary>Allowed time UOM for work centre capacity parameters and routing operation times (not material quantity UOM).</summary>
    public static class WorkCenterTimeUom
    {
        public const string Min = "Min";
        public const string Hr = "Hr";
        public const string Day = "Day";

        public static readonly string[] Allowed = { Min, Hr, Day };

        public static bool IsAllowed(string? value)
            => !string.IsNullOrWhiteSpace(value) && Allowed.Contains(value, StringComparer.Ordinal);

        /// <summary>Maps legacy / master-data UOM codes to Min, Hr, or Day; returns null if unknown.</summary>
        public static string? FromMeasurementCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var c = code.Trim();
            if (c.Equals(Min, StringComparison.OrdinalIgnoreCase)) return Min;
            if (c.Equals(Hr, StringComparison.OrdinalIgnoreCase)) return Hr;
            if (c.Equals(Day, StringComparison.OrdinalIgnoreCase)) return Day;
            var u = c.ToUpperInvariant();
            if (u is "M" or "MIN" or "MINUTE" or "MINUTES") return Min;
            if (u is "H" or "HR" or "HOUR" or "HOURS" or "HRS") return Hr;
            if (u is "D" or "DAY" or "DAYS") return Day;
            return null;
        }
    }
}
