using System.Globalization;

namespace AU_ERP.Services
{
    /// <summary>
    /// Keeps "current" counters aligned with from/to windows across Material, BP, and Document ranges.
    /// Current = last issued number; unset means the next issue will be the from/start value.
    /// </summary>
    public static class NumberRangeMaintenance
    {
        public static bool RangesOverlap(long fromA, long toA, long fromB, long toB) =>
            fromA <= toB && fromB <= toA;

        public static bool RangesOverlap(int fromA, int toA, int fromB, int toB) =>
            fromA <= toB && fromB <= toA;

        public static bool TryParseMaterialRange(string? fromNumber, string? toNumber, out long from, out long to, out string? error)
        {
            from = 0;
            to = 0;
            error = null;

            var fromRaw = (fromNumber ?? string.Empty).Trim();
            var toRaw = (toNumber ?? string.Empty).Trim();

            if (!long.TryParse(fromRaw, NumberStyles.None, CultureInfo.InvariantCulture, out from))
            {
                error = "Material range 'From number' must be a valid integer.";
                return false;
            }

            if (!long.TryParse(toRaw, NumberStyles.None, CultureInfo.InvariantCulture, out to))
            {
                error = "Material range 'To number' must be a valid integer.";
                return false;
            }

            if (to < from)
            {
                error = "Material range 'To number' cannot be smaller than 'From number'.";
                return false;
            }

            return true;
        }

        public static bool TryValidateBpRange(int startNumber, int endNumber, out string? error)
        {
            error = null;
            if (endNumber < startNumber)
            {
                error = "BP range end number cannot be smaller than start number.";
                return false;
            }
            return true;
        }

        public static bool TryValidateDocumentRange(int? fromNumber, int? toNumber, out string? error)
        {
            error = null;
            if (fromNumber.HasValue && toNumber.HasValue && toNumber.Value < fromNumber.Value)
            {
                error = "Document range 'To number' cannot be smaller than 'From number'.";
                return false;
            }
            return true;
        }

        /// <summary>
        /// True when the next issue would exceed <paramref name="toNumber"/> (finite cap required).
        /// Open-ended ranges (no parseable To) are never treated as exhausted here, so a second segment cannot be added for the same type until a cap exists and is exceeded.
        /// </summary>
        public static bool IsMaterialRangeExhausted(string? fromNumber, string? toNumber, string? currentNumber)
        {
            if (!long.TryParse((fromNumber ?? "").Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var from))
                return false;

            long? last = null;
            var cur = currentNumber?.Trim();
            if (!string.IsNullOrWhiteSpace(cur) && cur != "0")
            {
                if (long.TryParse(cur, NumberStyles.None, CultureInfo.InvariantCulture, out var v))
                    last = v;
            }

            var nextNumber = last.HasValue ? last.Value + 1 : from;

            var toStr = toNumber?.Trim();
            if (string.IsNullOrEmpty(toStr) || !long.TryParse(toStr, NumberStyles.None, CultureInfo.InvariantCulture, out var toNum))
                return false;

            return nextNumber > toNum;
        }

        public static string NormalizeMaterialLastIssued(string? fromNumber, string? toNumber, string? currentNumber)
        {
            var fromStr = fromNumber?.Trim() ?? "";
            if (!long.TryParse(fromStr, NumberStyles.None, CultureInfo.InvariantCulture, out var from))
                return BlankMaterialCurrent(currentNumber);

            long? to = null;
            var toStr = toNumber?.Trim();
            if (!string.IsNullOrEmpty(toStr) && long.TryParse(toStr, NumberStyles.None, CultureInfo.InvariantCulture, out var toVal))
                to = toVal;

            var last = ParseMaterialLastIssued(currentNumber);
            last = ClampLastIssued(from, to, last);
            return FormatMaterialLastIssued(last);
        }

        public static int NormalizeBpLastIssued(int startNumber, int endNumber, int currentNumber)
        {
            if (startNumber > endNumber)
                return 0;

            var last = currentNumber == 0 ? (int?)null : currentNumber;
            last = ClampLastIssuedInt(startNumber, endNumber, last);
            return last ?? 0;
        }

        public static int? NormalizeDocumentLastIssued(int? fromNumber, int? toNumber, int? currentNumber)
        {
            if (!fromNumber.HasValue)
                return currentNumber is null or 0 ? null : currentNumber;

            var from = fromNumber.Value;
            int? to = toNumber;
            var last = currentNumber is null or 0 ? (int?)null : currentNumber;
            last = ClampLastIssuedInt(from, to, last);
            return last;
        }

        private static string BlankMaterialCurrent(string? currentNumber)
        {
            if (string.IsNullOrWhiteSpace(currentNumber) || currentNumber.Trim() == "0")
                return "0";
            return currentNumber.Trim();
        }

        private static long? ParseMaterialLastIssued(string? currentNumber)
        {
            if (string.IsNullOrWhiteSpace(currentNumber) || currentNumber.Trim() == "0")
                return null;
            return long.TryParse(currentNumber.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        private static string FormatMaterialLastIssued(long? last)
            => last.HasValue ? last.Value.ToString(CultureInfo.InvariantCulture) : "0";

        private static long? ClampLastIssued(long from, long? to, long? lastIssued)
        {
            if (!lastIssued.HasValue)
                return null;
            if (lastIssued.Value < from)
                return null;
            if (to.HasValue && lastIssued.Value > to.Value)
                return to.Value;
            return lastIssued;
        }

        private static int? ClampLastIssuedInt(int from, int? to, int? lastIssued)
        {
            if (!lastIssued.HasValue)
                return null;
            if (lastIssued.Value < from)
                return null;
            if (to.HasValue && lastIssued.Value > to.Value)
                return to.Value;
            return lastIssued;
        }
    }
}
