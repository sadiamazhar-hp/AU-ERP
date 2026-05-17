using AU_ERP.Configuration;
using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>Warns admins when Configuration → Document → Integration rows are incomplete (prevents vague runtime failures).</summary>
public static class DocumentIntegrationDiagnostics
{
    private static DocumentRange? PickNextAssignableRange(IEnumerable<DocumentRange>? ranges)
    {
        if (ranges == null)
            return null;
        return ranges
            .Where(static r => r.FromNumber.HasValue && r.ToNumber.HasValue
                               && (r.CurrentNumber ?? (r.FromNumber!.Value - 1)) < r.ToNumber!.Value)
            .OrderBy(r => r.FromNumber)
            .FirstOrDefault();
    }

    private static bool HasConfiguredNumericRanges(IEnumerable<DocumentRange>? ranges) =>
        ranges?.Any(static r => r.FromNumber.HasValue && r.ToNumber.HasValue) == true;

    /// <returns>Human-readable warnings; empty list means every known module key has a usable active mapping.</returns>
    public static async Task<IReadOnlyList<string>> GetActiveIntegrationWarningsAsync(AppDbContext db, CancellationToken ct)
    {
        var warnings = new List<string>();

        var activeRows = await db.DocumentIntegrations.AsNoTracking()
            .Include(i => i.DocumentType)
                .ThenInclude(dt => dt!.DocumentRanges)
            .Where(i => i.IsActive)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var (moduleKey, display) in ModuleKeys.All)
        {
            var row = activeRows.FirstOrDefault(r =>
                string.Equals(r.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));
            if (row == null)
            {
                warnings.Add($"{display} ({moduleKey}): No active integration. Add one under Configuration → Document → Integration or features that allocate this document number will show an explicit error.");
                continue;
            }
            if (row.DocumentType is null)
            {
                warnings.Add($"{display}: Active integration points to a missing document type ID {row.DocumentTypeID}. Correct or delete this row.");
                continue;
            }
            var docCode = (row.DocumentType.DocCode ?? "").Trim();
            if (docCode.Length == 0)
            {
                warnings.Add($"{display}: The linked document type has no DocCode. Update Document types before issuing numbers.");
                continue;
            }
            if (!HasConfiguredNumericRanges(row.DocumentType.DocumentRanges))
            {
                warnings.Add($"{display}: The linked document type has no Document ranges. Add ranges under Configuration → Document → Document ranges.");
                continue;
            }
            if (PickNextAssignableRange(row.DocumentType.DocumentRanges) is null)
            {
                warnings.Add($"{display}: All Document ranges linked to this type are exhausted. Extend ranges under Configuration → Document → Document ranges.");
            }
        }

        return warnings;
    }

}
