using System.Globalization;
using AU_ERP.Models;

namespace AU_ERP.Services;

/// <summary>
/// Algorithms for issuing integrated document codes like SO-2000 (never backfills gaps; uses DB max suffix + stored counter).
/// </summary>
public static class DocumentIntegratedSequence
{
    public static long DefaultLastIssuedBeforeFirstIssue(long rangeStart) => rangeStart - 1;

    /// <summary>Parses the numeric suffix after "DocCode-" (case insensitive). Handles leading zeros.</summary>
    public static bool TryParseDocCodeSuffix(string docCodeTrimmed, string? documentNumber, out long suffix)
    {
        suffix = 0;
        if (string.IsNullOrWhiteSpace(documentNumber))
            return false;

        docCodeTrimmed = (docCodeTrimmed ?? "").Trim();
        if (docCodeTrimmed.Length == 0)
            return false;

        var trimmed = documentNumber.Trim();
        var prefix = docCodeTrimmed + "-";
        if (trimmed.Length <= prefix.Length
            || !trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var remainder = trimmed[prefix.Length..];
        return long.TryParse(remainder, NumberStyles.None, CultureInfo.InvariantCulture, out suffix)
               && remainder.Length > 0;
    }

    public static long MaxSuffixFromDocumentNumbers(IEnumerable<string?> candidates, string docCodeTrimmed)
    {
        var max = 0L;
        foreach (var raw in candidates)
        {
            if (TryParseDocCodeSuffix(docCodeTrimmed, raw, out var n) && n > max)
                max = n;
        }

        return max;
    }

    /// <summary>
    /// Finds the issuing range bucket and next number across ordered ranges using:
    /// next = MAX(lastIssuedStored, existingMaxDb) + 1, bumped to RangeStart when below window; never consumes gaps lower than MAX.
    /// </summary>
    public static bool TryPickNextAcrossOrderedRanges(
        IReadOnlyList<DocumentRange> orderedRanges,
        long maxExistingDbSuffix,
        out DocumentRange? chosenRange,
        out long nextNumber)
    {
        chosenRange = null;
        nextNumber = 0;
        foreach (var r in orderedRanges)
        {
            if (!r.FromNumber.HasValue || !r.ToNumber.HasValue)
                continue;

            var from = r.FromNumber!.Value;
            var to = r.ToNumber!.Value;
            var lastStored = r.LastIssuedNumber ?? DefaultLastIssuedBeforeFirstIssue(from);
            long candidate = Math.Max(lastStored, maxExistingDbSuffix) + 1;
            if (candidate < from)
                candidate = from;
            if (candidate <= to)
            {
                chosenRange = r;
                nextNumber = candidate;
                return true;
            }
        }

        return false;
    }
}
