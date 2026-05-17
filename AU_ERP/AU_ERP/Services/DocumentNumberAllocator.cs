using System.Data;
using AU_ERP.Configuration;
using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>
/// Issues integrated codes (e.g. SO-2001) under Document Integration ranges.
/// Concurrency: callers should ideally run allocation inside their own Serializable transaction when multiple DB writes participate;
/// when no ambient transaction exists, allocator begins one with <see cref="IsolationLevel.Serializable"/> so parallel issuers serialize.
/// </summary>
public sealed class DocumentNumberAllocator
{
    private readonly AppDbContext _db;

    public DocumentNumberAllocator(AppDbContext db) => _db = db;

    public async Task<string> AllocateAsync(string moduleKey, CancellationToken ct = default)
    {
        var ambient = _db.Database.CurrentTransaction;
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? ownTx = null;
        if (ambient is null)
        {
            ownTx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                .ConfigureAwait(false);
        }

        try
        {
            var integ = await _db.DocumentIntegrations
                .Include(i => i.DocumentType).ThenInclude(t => t!.DocumentRanges)
                .FirstOrDefaultAsync(i => i.ModuleKey == moduleKey && i.IsActive, ct)
                .ConfigureAwait(false);

            if (integ?.DocumentType is null)
                throw DocumentIntegrationMissingException.NotConfigured(moduleKey);

            var docCode = (integ.DocumentType.DocCode ?? "").Trim();
            if (string.IsNullOrEmpty(docCode))
                throw DocumentIntegrationMissingException.DocCodeMissing(moduleKey);

            if (!HasConfiguredNumericRanges(integ.DocumentType.DocumentRanges))
                throw DocumentIntegrationMissingException.NoRangesAssigned(moduleKey);

            var rangesOrdered = integ.DocumentType.DocumentRanges!
                .Where(static r => r.FromNumber.HasValue && r.ToNumber.HasValue)
                .OrderBy(r => r.FromNumber)
                .ToList();

            var maxExisting = await IntegratedDocumentIssuedNumbersMaxSuffix
                .GetMaxSuffixAsync(_db, docCode, moduleKey, ct)
                .ConfigureAwait(false);

            if (!DocumentIntegratedSequence.TryPickNextAcrossOrderedRanges(
                    rangesOrdered, maxExisting, out var chosenRange, out var next))
                throw new DocumentIntegrationRangeExhaustedException(moduleKey);

            chosenRange!.LastIssuedNumber = next;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            if (ownTx is not null)
                await ownTx.CommitAsync(ct).ConfigureAwait(false);

            return $"{docCode}-{next}";
        }
        catch
        {
            if (ownTx is not null)
                await ownTx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Computes what would be issued next without committing (best-effort; race with concurrent issuers is possible).</summary>
    public async Task<string> PeekNextAsync(string moduleKey, CancellationToken ct = default)
    {
        var integ = await _db.DocumentIntegrations.AsNoTracking()
            .Include(i => i.DocumentType).ThenInclude(t => t!.DocumentRanges)
            .FirstOrDefaultAsync(i => i.ModuleKey == moduleKey && i.IsActive, ct)
            .ConfigureAwait(false);

        if (integ?.DocumentType is null)
            throw DocumentIntegrationMissingException.NotConfigured(moduleKey);

        var docCode = (integ.DocumentType.DocCode ?? "").Trim();
        if (string.IsNullOrEmpty(docCode))
            throw DocumentIntegrationMissingException.DocCodeMissing(moduleKey);

        if (!HasConfiguredNumericRanges(integ.DocumentType.DocumentRanges))
            throw DocumentIntegrationMissingException.NoRangesAssigned(moduleKey);

        var rangesOrdered = integ.DocumentType.DocumentRanges!
            .Where(static r => r.FromNumber.HasValue && r.ToNumber.HasValue)
            .OrderBy(r => r.FromNumber)
            .ToList();

        var maxExisting = await IntegratedDocumentIssuedNumbersMaxSuffix
            .GetMaxSuffixAsync(_db, docCode, moduleKey, ct)
            .ConfigureAwait(false);

        if (!DocumentIntegratedSequence.TryPickNextAcrossOrderedRanges(rangesOrdered, maxExisting, out _, out var next))
            throw new DocumentIntegrationRangeExhaustedException(moduleKey);

        return $"{docCode}-{next}";
    }

    private static bool HasConfiguredNumericRanges(IEnumerable<DocumentRange>? ranges) =>
        ranges?.Any(static r => r.FromNumber.HasValue && r.ToNumber.HasValue) == true;
}

public sealed class DocumentIntegrationMissingException : Exception
{
    public string ModuleKey { get; }

    private DocumentIntegrationMissingException(string moduleKey, string message)
        : base(message)
    {
        ModuleKey = moduleKey;
    }

    /// <summary>No active Integration row / missing document type linkage.</summary>
    public static DocumentIntegrationMissingException NotConfigured(string moduleKey)
    {
        var d = ModuleKeys.GetDisplayName(moduleKey);
        return new DocumentIntegrationMissingException(moduleKey,
            $"{d} is not integrated under Configuration → Document → Integration with a Document. Add an active row for this module with a Document type and Document ranges.");
    }

    public static DocumentIntegrationMissingException DocCodeMissing(string moduleKey)
    {
        var d = ModuleKeys.GetDisplayName(moduleKey);
        return new DocumentIntegrationMissingException(moduleKey,
            $"{d} is linked under Document Integration but the Document type has no DocCode. Set DocCode under Configuration → Document → Document types.");
    }

    public static DocumentIntegrationMissingException NoRangesAssigned(string moduleKey)
    {
        var d = ModuleKeys.GetDisplayName(moduleKey);
        return new DocumentIntegrationMissingException(moduleKey,
            $"{d} is linked under Document Integration but its Document type has no Document ranges. Add ranges under Configuration → Document → Document ranges.");
    }
}

public sealed class DocumentIntegrationRangeExhaustedException : Exception
{
    public string ModuleKey { get; }

    public DocumentIntegrationRangeExhaustedException(string moduleKey)
        : base(BuildRangeMessage(moduleKey))
    {
        ModuleKey = moduleKey;
    }

    private static string BuildRangeMessage(string moduleKey)
    {
        var d = ModuleKeys.GetDisplayName(moduleKey);
        return $"{d} cannot allocate the next document number — all configured ranges are exhausted. Extend or add ranges under Configuration → Document → Document ranges.";
    }
}
