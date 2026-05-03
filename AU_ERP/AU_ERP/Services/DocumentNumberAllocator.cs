using System.Data;
using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

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
                throw new DocumentIntegrationMissingException(moduleKey);

            var docCode = (integ.DocumentType.DocCode ?? "").Trim();
            if (string.IsNullOrEmpty(docCode))
                throw new DocumentIntegrationMissingException(moduleKey, "Document type has no DocCode configured.");

            var range = PickNextRange(integ.DocumentType.DocumentRanges);
            if (range is null)
                throw new DocumentIntegrationRangeExhaustedException(moduleKey);

            var next = NextSerial(range);
            range.CurrentNumber = next;
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

    /// <summary>Computes the next number that would be issued without updating the database.</summary>
    public async Task<string> PeekNextAsync(string moduleKey, CancellationToken ct = default)
    {
        var integ = await _db.DocumentIntegrations.AsNoTracking()
            .Include(i => i.DocumentType).ThenInclude(t => t!.DocumentRanges)
            .FirstOrDefaultAsync(i => i.ModuleKey == moduleKey && i.IsActive, ct)
            .ConfigureAwait(false);

        if (integ?.DocumentType is null)
            throw new DocumentIntegrationMissingException(moduleKey);

        var docCode = (integ.DocumentType.DocCode ?? "").Trim();
        if (string.IsNullOrEmpty(docCode))
            throw new DocumentIntegrationMissingException(moduleKey, "Document type has no DocCode configured.");

        var range = PickNextRange(integ.DocumentType.DocumentRanges);
        if (range is null)
            throw new DocumentIntegrationRangeExhaustedException(moduleKey);

        var next = NextSerial(range);
        return $"{docCode}-{next}";
    }

    private static DocumentRange? PickNextRange(IEnumerable<DocumentRange>? ranges)
    {
        if (ranges == null)
            return null;
        return ranges
            .Where(r => r.FromNumber.HasValue && r.ToNumber.HasValue
                        && (r.CurrentNumber ?? (r.FromNumber!.Value - 1)) < r.ToNumber!.Value)
            .OrderBy(r => r.FromNumber)
            .FirstOrDefault();
    }

    private static int NextSerial(DocumentRange range)
    {
        var from = range.FromNumber!.Value;
        var last = range.CurrentNumber ?? (from - 1);
        return Math.Max(from, last + 1);
    }
}

public sealed class DocumentIntegrationMissingException : Exception
{
    public string ModuleKey { get; }

    public DocumentIntegrationMissingException(string moduleKey, string? detail = null)
        : base(detail ?? $"No active document integration is configured for module '{moduleKey}'. Configure it under Configuration → Document → Integration.")
    {
        ModuleKey = moduleKey;
    }
}

public sealed class DocumentIntegrationRangeExhaustedException : Exception
{
    public string ModuleKey { get; }

    public DocumentIntegrationRangeExhaustedException(string moduleKey)
        : base($"Document number range is exhausted for module '{moduleKey}'. Add or extend document ranges for the linked document type.")
    {
        ModuleKey = moduleKey;
    }
}
