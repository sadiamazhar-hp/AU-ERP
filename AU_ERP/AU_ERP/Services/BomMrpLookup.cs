using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>Shared read-only BOM option queries for MRP, production order, and cross-module lookups.</summary>
public static class BomMrpLookup
{
    public sealed class MrpBomOptionRow
    {
        public int BomId { get; init; }
        public string? BomCode { get; init; }
        public string? BomTitle { get; init; }
        public string Plant { get; init; } = "";
        public DateTime? ValidFrom { get; init; }
        public bool IsDefaultBom { get; init; }
    }

    /// <summary>
    /// Active MRP-valid BOM headers for a material.
    /// When <paramref name="headerMaterialTypeCode"/> is set, results are narrowed to that header type (HALB/FERT).
    /// </summary>
    public static async Task<List<MrpBomOptionRow>> GetOptionsAsync(
        AppDbContext db,
        string materialNumber,
        string? headerMaterialTypeCode = null,
        DateTime? asOfDate = null,
        CancellationToken ct = default)
    {
        var mat = (materialNumber ?? "").Trim();
        if (mat.Length == 0)
            return new List<MrpBomOptionRow>();

        var day = (asOfDate ?? DateTime.Today).Date;
        var hdrType = (headerMaterialTypeCode ?? "").Trim().ToUpperInvariant();

        IQueryable<BomHeadersSample> query = db.BomHeadersSamples.AsNoTracking()
            .Where(h => h.BomMaterialNumber == mat);

        if (hdrType.Length > 0)
            query = query.Where(h => h.HeaderMaterialTypeCode == hdrType);

        return await query
            .ForMrpSelection(day)
            .OrderForMrpSelection()
            .Select(h => new MrpBomOptionRow
            {
                BomId = h.BomID,
                BomCode = h.BOMCode,
                BomTitle = h.BOMTitle,
                Plant = h.Plant ?? "",
                ValidFrom = h.ValidFrom,
                IsDefaultBom = h.IsDefaultBom
            })
            .ToListAsync(ct);
    }
}
