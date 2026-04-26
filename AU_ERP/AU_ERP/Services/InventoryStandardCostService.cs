using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>Resolves cost or sales list price per a given UOM from inventory lines or material master (per-base grade) + unit conversion.</summary>
public static class InventoryStandardCostService
{
    /// <param name="priceGrade">A, B, C, or Scrap (see <see cref="StockInventoryGradeCodes"/>). Defaults to first quality (A).</param>
    public static async Task<decimal> ResolveStandardCostPerUomAsync(
        AppDbContext db,
        string materialNumber,
        int uomId,
        CancellationToken cancellationToken = default,
        string? priceGrade = null)
    {
        var key = (materialNumber ?? "").Trim();
        if (key.Length == 0 || uomId <= 0)
            return 0;

        var fromInv = await db.StockInventoryLines.AsNoTracking()
            .Where(s => s.MaterialNumber == key
                        && s.QuantityUomId == uomId
                        && s.Status == StockInventoryLine.StatusActive)
            .Select(s => s.StandardCostPerUom)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (fromInv.Count > 0)
            return fromInv.Average(s => s);

        var mat = await db.CreateMaterialMaster.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MaterialNumber == key, cancellationToken)
            .ConfigureAwait(false);

        var perBase = GetMaterialPricePerBaseUom(mat, priceGrade);
        if (perBase is not decimal b || b <= 0)
            return 0;

        var toBase = await UnitConversionMath.ToBaseAsync(db, key, 1, uomId, cancellationToken)
            .ConfigureAwait(false);
        if (!toBase.ok)
            return 0;

        return toBase.quantityBase * b;
    }

    /// <summary>PKR per base UOM for the given grade; falls back to Grade A if unknown or null.</summary>
    public static decimal? GetMaterialPricePerBaseUom(CreateMaterialMaster? mat, string? priceGrade)
    {
        if (mat == null) return null;
        var g = (priceGrade ?? StockInventoryGradeCodes.FirstQuality).Trim();
        if (g.Equals(StockInventoryGradeCodes.SecondQuality, StringComparison.OrdinalIgnoreCase) || g.Equals("B", StringComparison.OrdinalIgnoreCase))
            return mat.SalesPriceGradeBPerBaseUom;
        if (g.Equals(StockInventoryGradeCodes.ThirdQuality, StringComparison.OrdinalIgnoreCase) || g.Equals("C", StringComparison.OrdinalIgnoreCase))
            return mat.SalesPriceGradeCPerBaseUom;
        if (g.Equals(StockInventoryGradeCodes.Scrap, StringComparison.OrdinalIgnoreCase))
            return mat.ScrapCostPerBaseUom;
        return mat.SalesPriceGradeAPerBaseUom;
    }
}
