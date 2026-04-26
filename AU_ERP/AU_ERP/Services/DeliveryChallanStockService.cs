using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>Consume stock for delivery challan lines (FIFO by UpdatedAt) and return batch lot text from source rows.</summary>
public static class DeliveryChallanStockService
{
    public sealed class LineDeduct
    {
        public required string MaterialNumber { get; init; }
        public decimal Qty { get; init; }
        public int QuantityUomId { get; init; }
        public int? SalesOrderItemId { get; init; }
    }

    public static string InventoryGradeFromSalesGrade(string? salesGrade)
    {
        var g = (salesGrade ?? "").Trim();
        if (g.Length == 0 || g.Equals("A", StringComparison.OrdinalIgnoreCase))
            return StockInventoryGradeCodes.FirstQuality;
        if (g.Equals("B", StringComparison.OrdinalIgnoreCase) || g.Equals(StockInventoryGradeCodes.SecondQuality, StringComparison.OrdinalIgnoreCase))
            return StockInventoryGradeCodes.SecondQuality;
        if (g.Equals("C", StringComparison.OrdinalIgnoreCase) || g.Equals(StockInventoryGradeCodes.ThirdQuality, StringComparison.OrdinalIgnoreCase))
            return StockInventoryGradeCodes.ThirdQuality;
        if (g.Equals("Scrap", StringComparison.OrdinalIgnoreCase) || g.Equals(StockInventoryGradeCodes.Scrap, StringComparison.OrdinalIgnoreCase))
            return StockInventoryGradeCodes.Scrap;
        return StockInventoryGradeCodes.FirstQuality;
    }

    public static async Task<(bool ok, string? error, IReadOnlyList<string?> Batches)>
        DeductAndGetBatchLabelsAsync(
            AppDbContext db,
            IReadOnlyList<LineDeduct> lines,
            IReadOnlyDictionary<int, string?>? salesItemGradeById,
            CancellationToken ct = default)
    {
        var batches = new string?[lines.Count];
        for (var li = 0; li < lines.Count; li++)
        {
            var line = lines[li];
            var mat = (line.MaterialNumber ?? "").Trim();
            if (mat.Length == 0)
                return (false, "Line has no material.", batches);
            if (line.QuantityUomId <= 0)
                return (false, "Each line must have a valid unit of measure.", batches);
            if (line.Qty <= 0)
                return (false, "Each line must have a positive delivery quantity.", batches);

            string grade = StockInventoryGradeCodes.FirstQuality;
            if (line.SalesOrderItemId is int soiId && salesItemGradeById != null
                && salesItemGradeById.TryGetValue(soiId, out var g))
                grade = InventoryGradeFromSalesGrade(g);

            var lotParts = new List<string>();
            var remaining = line.Qty;

            var stockRows = await db.StockInventoryLines
                .Where(s =>
                    s.MaterialNumber == mat
                    && s.Status == StockInventoryLine.StatusActive
                    && s.Grade == grade
                    && s.Quantity > 0)
                .OrderBy(s => s.UpdatedAt)
                .ThenBy(s => s.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var st in stockRows)
            {
                if (remaining <= 0) break;
                var (convOk, needInStUom, cErr) = await UnitConversionMath.ConvertAsync(
                    db, mat, remaining, line.QuantityUomId, st.QuantityUomId, ct)
                    .ConfigureAwait(false);
                if (!convOk)
                    return (false, cErr ?? "Unit conversion failed for stock issue.", batches);
                if (needInStUom <= 0) continue;
                var take = Math.Min(st.Quantity, needInStUom);
                st.Quantity = Math.Round(st.Quantity - take, 4, MidpointRounding.AwayFromZero);
                st.StockValue = Math.Round(st.Quantity * st.StandardCostPerUom, 2, MidpointRounding.AwayFromZero);
                st.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(st.BatchOrLot))
                    lotParts.Add(st.BatchOrLot.Trim());
                else
                    lotParts.Add("INV-" + st.Id);
                var (backOk, backDocQty, backErr) = await UnitConversionMath.ConvertAsync(
                    db, mat, take, st.QuantityUomId, line.QuantityUomId, ct)
                    .ConfigureAwait(false);
                if (!backOk)
                    return (false, backErr ?? "Unit conversion back to delivery UOM failed.", batches);
                remaining = Math.Round(remaining - backDocQty, 4, MidpointRounding.AwayFromZero);
            }

            if (remaining > 0.0001m)
            {
                return (false, $"Insufficient stock for material {mat} (grade {grade}, UOM id {line.QuantityUomId}). Missing about {remaining:0.####}.", batches);
            }

            if (lotParts.Count == 0)
                batches[li] = null;
            else
                batches[li] = string.Join(" + ", lotParts.Distinct(StringComparer.Ordinal));
        }

        return (true, null, batches);
    }
}
