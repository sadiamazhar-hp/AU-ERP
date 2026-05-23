using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>Resolves ROH component material numbers from active BOM for a finished/semi-finished material (sales return QI convert target).</summary>
public sealed class SalesReturnQiBomService
{
    private readonly AppDbContext _db;

    public SalesReturnQiBomService(AppDbContext db) => _db = db;

    public sealed record RohReceiptLine(string ComponentMaterialNumber, decimal QtyInBaseUom, int BaseUomId);

    public async Task<List<string>> ListRawComponentMaterialNumbersAsync(string finishedGoodsMaterialNumber, CancellationToken ct = default)
    {
        var key = (finishedGoodsMaterialNumber ?? "").Trim();
        if (key.Length == 0)
            return new List<string>();

        var matEntity = await _db.CreateMaterialMaster.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MaterialNumber == key, ct)
            .ConfigureAwait(false);
        var mt = (matEntity?.MaterialTypeCode ?? "").Trim().ToUpperInvariant();
        if (mt != "FERT" && mt != "HALB")
            return new List<string>();

        var bom = await MrpExplosionService.ResolveBomHeaderAsync(_db, key, mt, null, ct).ConfigureAwait(false);
        if (bom == null)
            return new List<string>();

        var componentNumbers = await _db.BomItemsSamples.AsNoTracking()
            .Where(i => i.BomID == bom.BomID && i.MaterialNumber != null && i.MaterialNumber != "")
            .Select(i => i.MaterialNumber!.Trim())
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (componentNumbers.Count == 0)
            return new List<string>();

        var mats = await _db.CreateMaterialMaster.AsNoTracking()
            .Where(m => componentNumbers.Contains(m.MaterialNumber))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return mats
            .Where(m => string.Equals(m.MaterialTypeCode?.Trim(), "ROH", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.MaterialNumber)
            .Distinct()
            .OrderBy(m => m)
            .ToList();
    }

    /// <summary>
    /// Scales active BOM ROH components by converted FG quantity (same factor logic as MRP explosion).
    /// Returns each component quantity in that material's base UOM.
    /// </summary>
    public async Task<(bool ok, string? error, IReadOnlyList<RohReceiptLine> lines)> ExplodeRohForFgConvertAsync(
        string fgMaterialNumber,
        decimal qtyConvertInFgUom,
        int fgUomId,
        CancellationToken ct = default)
    {
        if (qtyConvertInFgUom <= 0)
            return (true, null, Array.Empty<RohReceiptLine>());

        var key = (fgMaterialNumber ?? "").Trim();
        if (key.Length == 0)
            return (false, "Finished material is required.", Array.Empty<RohReceiptLine>());

        var fgMat = await _db.CreateMaterialMaster.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MaterialNumber == key, ct)
            .ConfigureAwait(false);
        if (fgMat == null)
            return (false, $"Material '{key}' not found.", Array.Empty<RohReceiptLine>());

        var mt = (fgMat.MaterialTypeCode ?? "").Trim().ToUpperInvariant();
        if (mt != "FERT" && mt != "HALB")
            return (false, $"Convert to raw requires a FERT or HALB material; '{key}' is {mt}.", Array.Empty<RohReceiptLine>());

        var header = await MrpExplosionService.ResolveBomHeaderAsync(_db, key, mt, null, ct).ConfigureAwait(false);
        if (header == null)
            return (false, $"No active BOM found for '{key}' ({mt}) to explode convert quantity.", Array.Empty<RohReceiptLine>());

        var (okFgBase, qtyFgBase, fgErr) = await UnitConversionMath.ToBaseAsync(_db, key, qtyConvertInFgUom, fgUomId, ct)
            .ConfigureAwait(false);
        if (!okFgBase)
            return (false, fgErr ?? $"Cannot convert FG quantity to base UOM for '{key}'.", Array.Empty<RohReceiptLine>());

        var baseQty = header.BaseQty is > 0 ? header.BaseQty.Value : 1m;
        var factor = qtyFgBase / baseQty;

        var items = await _db.BomItemsSamples.AsNoTracking()
            .Where(i => i.BomID == header.BomID)
            .Include(i => i.Uom)
            .Include(i => i.CreateMaterialMaster)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var acc = new Dictionary<string, (decimal qtyBase, int baseUomId)>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var compNum = item.MaterialNumber?.Trim();
            if (string.IsNullOrEmpty(compNum))
                continue;

            var compMat = item.CreateMaterialMaster
                ?? await _db.CreateMaterialMaster.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MaterialNumber == compNum, ct)
                    .ConfigureAwait(false);
            if (compMat == null)
                continue;

            var compType = (compMat.MaterialTypeCode ?? "").Trim().ToUpperInvariant();
            if (compType != "ROH")
                continue;

            var itemQty = item.Quantity ?? 0m;
            var scrapPct = item.ScrapPercentage ?? 0m;
            var requiredInBomLineUom = factor * itemQty * (1m + scrapPct / 100m);
            if (requiredInBomLineUom <= 0)
                continue;

            var lineUomId = item.UomId;
            if (lineUomId is null or <= 0)
            {
                var (cb, _) = await MaterialUomForMaterialHelper.ResolveBaseUomIdAsync(_db, compMat, ct).ConfigureAwait(false);
                if (cb == null)
                    return (false, $"Component '{compNum}' has no BOM line UOM and no base UOM on material master.", Array.Empty<RohReceiptLine>());

                lineUomId = cb;
            }

            var (okBase, qtyBase, cErr) = await UnitConversionMath.ToBaseAsync(_db, compNum, requiredInBomLineUom, lineUomId.Value, ct)
                .ConfigureAwait(false);
            if (!okBase)
                return (false, cErr ?? $"Unit conversion failed for BOM component '{compNum}'.", Array.Empty<RohReceiptLine>());

            var (baseUomId, baseErr) = await MaterialUomForMaterialHelper.ResolveBaseUomIdAsync(_db, compMat, ct).ConfigureAwait(false);
            if (baseUomId == null)
                return (false, baseErr ?? $"Base UOM missing for component '{compNum}'.", Array.Empty<RohReceiptLine>());

            var rounded = Math.Round(qtyBase, 4, MidpointRounding.AwayFromZero);
            if (rounded <= 0)
                continue;

            if (acc.TryGetValue(compNum, out var existing))
            {
                if (existing.baseUomId != baseUomId.Value)
                    return (false, $"BOM lists inconsistent base UOM for component '{compNum}'.", Array.Empty<RohReceiptLine>());
                acc[compNum] = (existing.qtyBase + rounded, baseUomId.Value);
            }
            else
            {
                acc[compNum] = (rounded, baseUomId.Value);
            }
        }

        if (acc.Count == 0)
            return (false, $"BOM for '{key}' has no ROH components to receive for convert quantity.", Array.Empty<RohReceiptLine>());

        var lines = acc.Select(kv => new RohReceiptLine(kv.Key, kv.Value.qtyBase, kv.Value.baseUomId)).ToList();
        return (true, null, lines);
    }
}
