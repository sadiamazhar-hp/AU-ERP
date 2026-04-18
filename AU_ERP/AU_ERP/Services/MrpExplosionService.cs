using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;

namespace AU_ERP.Services
{
    public static class MrpExplosionService
    {
        private static readonly decimal Epsilon = 0.0001m;

        private static readonly JsonSerializerOptions BomSnapshotJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private static readonly JsonSerializerOptions BomSnapshotReadOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>User-facing summary when <see cref="MrpRunResponseDto.AllSatisfied"/> is false.</summary>
        public static string FormatShortageMessage(MrpRunResponseDto mrp, int maxLines = 12)
        {
            var rows = (mrp.Rows ?? []).Where(r => r.Shortage).Take(maxLines).ToList();
            if (rows.Count == 0)
                return mrp.Message ?? "Material requirements are not satisfied for release.";
            var parts = rows.Select(r =>
                $"{r.MaterialNumber} (required {r.RequiredQty:0.####} {r.RequiredUomCode ?? ""}, on hand {r.OnHandQty:0.####})");
            var msg = "Cannot release: component shortage — " + string.Join("; ", parts);
            if ((mrp.Rows ?? []).Count(r => r.Shortage) > maxLines)
                msg += " …";
            return msg;
        }

        /// <summary>
        /// <see cref="BomHeadersSample.BaseQty"/> is treated as expressed in the header material's <strong>base UOM</strong>
        /// (same convention as material master base unit).
        /// </summary>
        public static async Task<MrpRunResponseDto> RunAsync(
            AppDbContext db,
            string materialNumber,
            decimal quantity,
            int uomId,
            bool requireFertMaterialOnly,
            CancellationToken ct = default)
        {
            var key = materialNumber.Trim();
            var resp = new MrpRunResponseDto { Success = false };

            if (string.IsNullOrEmpty(key))
            {
                resp.Message = "Material is required.";
                return resp;
            }

            if (quantity <= 0)
            {
                resp.Message = "Quantity must be greater than zero.";
                return resp;
            }

            if (uomId <= 0)
            {
                resp.Message = "UOM is required.";
                return resp;
            }

            var mat = await db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == key, ct);
            if (mat == null)
            {
                resp.Message = "Material not found.";
                return resp;
            }

            var mt = (mat.MaterialTypeCode ?? "").Trim().ToUpperInvariant();
            if (requireFertMaterialOnly && mt != "FERT")
            {
                resp.Message = "MRP run is only allowed for finished goods (FERT).";
                return resp;
            }

            if (mt != "FERT" && mt != "HALB")
            {
                resp.Message = "MRP can only run for materials of type FERT or HALB.";
                return resp;
            }

            var uomErr = await MaterialUomForMaterialHelper.ValidateUomForMaterialAsync(db, key, uomId, ct);
            if (uomErr != null)
            {
                resp.Message = uomErr;
                return resp;
            }

            var (okReqBase, qtyInBase, convErr) = await UnitConversionMath.ToBaseAsync(db, key, quantity, uomId, ct);
            if (!okReqBase)
            {
                resp.Message = convErr ?? "Could not convert requested quantity to base UOM.";
                return resp;
            }

            var header = await db.BomHeadersSamples.AsNoTracking()
                .Where(h => h.BomMaterialNumber == key && h.HeaderMaterialTypeCode == mt)
                .OrderByDescending(h => h.ValidFrom)
                .ThenByDescending(h => h.BomID)
                .FirstOrDefaultAsync(ct);

            if (header == null)
            {
                resp.Message = $"No bill of materials found for material '{key}' with header type {mt}.";
                return resp;
            }

            var baseQty = header.BaseQty is > 0 ? header.BaseQty.Value : 1m;
            var factor = qtyInBase / baseQty;

            var items = await db.BomItemsSamples.AsNoTracking()
                .Where(i => i.BomID == header.BomID)
                .Include(i => i.Uom)
                .Include(i => i.CreateMaterialMaster)
                .ToListAsync(ct);

            if (items.Count == 0)
            {
                resp.Success = true;
                resp.Message = "BOM has no components.";
                resp.AllSatisfied = false;
                return resp;
            }

            var rows = new List<MrpRunResultRowDto>();
            var warnings = new List<string>();

            foreach (var item in items)
            {
                var compNum = item.MaterialNumber?.Trim();
                if (string.IsNullOrEmpty(compNum))
                    continue;

                var compMat = item.CreateMaterialMaster
                    ?? await db.CreateMaterialMaster.AsNoTracking()
                        .FirstOrDefaultAsync(m => m.MaterialNumber == compNum, ct);
                if (compMat == null)
                {
                    warnings.Add($"Component '{compNum}' is not on file; skipped.");
                    continue;
                }

                var compType = (compMat.MaterialTypeCode ?? "").Trim().ToUpperInvariant();

                var itemQty = item.Quantity ?? 0m;
                var scrapPct = item.ScrapPercentage ?? 0m;
                var required = factor * itemQty * (1m + scrapPct / 100m);

                var lineUomId = item.UomId;
                if (lineUomId is null or <= 0)
                {
                    var (cb, _) = await MaterialUomForMaterialHelper.ResolveBaseUomIdAsync(db, compMat, ct);
                    if (cb == null)
                    {
                        warnings.Add($"Component '{compNum}' has no line UOM and no base UOM: skipped.");
                        continue;
                    }

                    lineUomId = cb;
                }

                var uomCode = item.Uom?.Code ?? await db.UnitOfMeasurements.AsNoTracking()
                    .Where(u => u.Id == lineUomId.Value)
                    .Select(u => u.Code)
                    .FirstOrDefaultAsync(ct);

                var stockLines = await db.StockInventoryLines.AsNoTracking()
                    .Where(s => s.MaterialNumber == compNum && s.Status == StockInventoryLine.StatusActive)
                    .Select(s => new { s.Quantity, s.QuantityUomId })
                    .ToListAsync(ct);

                decimal onHandInLineUom = 0;
                foreach (var sl in stockLines)
                {
                    var (cok, qConv, _) = await UnitConversionMath.ConvertAsync(
                        db, compNum, sl.Quantity, sl.QuantityUomId, lineUomId.Value, ct);
                    if (cok)
                        onHandInLineUom += qConv;
                    else
                        warnings.Add($"Inventory line for '{compNum}' could not convert UOM to BOM line UOM; treated as 0.");
                }

                var shortage = onHandInLineUom + Epsilon < required;

                var hasHalbBom = compType == "HALB" && await db.BomHeadersSamples.AsNoTracking()
                    .AnyAsync(h => h.BomMaterialNumber == compNum && h.HeaderMaterialTypeCode == "HALB", ct);

                var row = new MrpRunResultRowDto
                {
                    MaterialNumber = compNum,
                    Description = compMat.Description,
                    MaterialTypeCode = compType,
                    RequiredQty = decimal.Round(required, 4, MidpointRounding.AwayFromZero),
                    RequiredUomCode = uomCode,
                    RequiredUomId = lineUomId.Value,
                    OnHandQty = decimal.Round(onHandInLineUom, 4, MidpointRounding.AwayFromZero),
                    Shortage = shortage,
                    CanRunSubMrp = hasHalbBom,
                    ShowPurchaseStub = shortage && (compType == "ROH" || compType == "VERP")
                };
                rows.Add(row);
            }

            resp.Success = true;
            resp.Rows = rows;
            resp.Warnings = warnings;
            resp.AllSatisfied = rows.Count > 0 && rows.TrueForAll(r => !r.Shortage);
            resp.Message = null;
            return resp;
        }

        /// <summary>Resolve BOM header for a material (HALB or FERT header type must match material type).</summary>
        public static async Task<BomHeadersSample?> ResolveBomHeaderAsync(
            AppDbContext db,
            string materialNumber,
            string headerMaterialTypeCode,
            CancellationToken ct = default)
        {
            var key = materialNumber.Trim();
            var mt = headerMaterialTypeCode.Trim().ToUpperInvariant();
            return await db.BomHeadersSamples.AsNoTracking()
                .Where(h => h.BomMaterialNumber == key && h.HeaderMaterialTypeCode == mt)
                .OrderByDescending(h => h.ValidFrom)
                .ThenByDescending(h => h.BomID)
                .FirstOrDefaultAsync(ct);
        }

        /// <summary>Static BOM lines (quantities as stored on BOM, not scaled by production order).</summary>
        public static async Task<List<MrpBomLineDisplayDto>> GetBomLinesForMaterialAsync(
            AppDbContext db,
            string materialNumber,
            CancellationToken ct = default)
        {
            var key = materialNumber.Trim();
            var mat = await db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == key, ct);
            if (mat == null)
                return new List<MrpBomLineDisplayDto>();

            var mt = (mat.MaterialTypeCode ?? "").Trim().ToUpperInvariant();
            if (mt != "FERT" && mt != "HALB")
                return new List<MrpBomLineDisplayDto>();

            var header = await ResolveBomHeaderAsync(db, key, mt, ct);
            if (header == null)
                return new List<MrpBomLineDisplayDto>();

            var items = await db.BomItemsSamples.AsNoTracking()
                .Where(i => i.BomID == header.BomID)
                .Include(i => i.Uom)
                .Include(i => i.CreateMaterialMaster)
                .OrderBy(i => i.ItemID)
                .ToListAsync(ct);

            return items
                .Where(i => !string.IsNullOrWhiteSpace(i.MaterialNumber))
                .Select(i => new MrpBomLineDisplayDto
                {
                    MaterialNumber = i.MaterialNumber!.Trim(),
                    Description = i.CreateMaterialMaster?.Description,
                    MaterialTypeCode = i.CreateMaterialMaster?.MaterialTypeCode,
                    Quantity = i.Quantity ?? 0m,
                    UomCode = i.Uom?.Code
                })
                .ToList();
        }

        /// <summary>
        /// Scaled BOM lines from the <strong>current</strong> material BOM (ignores any released snapshot on the order).
        /// </summary>
        public static async Task<(bool success, string? error, List<MrpBomLineDisplayDto> lines)> BuildScaledBomLinesFromLiveBomAsync(
            AppDbContext db,
            ProductionOrder order,
            CancellationToken ct = default)
        {
            var lines = new List<MrpBomLineDisplayDto>();
            var key = order.FinishedMaterialNumber.Trim();
            var mat = await db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == key, ct);
            if (mat == null)
                return (false, "Finished material not found.", lines);

            var mt = (mat.MaterialTypeCode ?? "").Trim().ToUpperInvariant();
            if (mt != "FERT" && mt != "HALB")
                return (true, null, lines);

            var header = await ResolveBomHeaderAsync(db, key, mt, ct);
            if (header == null)
                return (false, $"No bill of materials found for '{key}' (type {mt}).", lines);

            var baseQty = header.BaseQty is > 0 ? header.BaseQty.Value : 1m;

            var (okBase, qtyInBase, convErr) = await UnitConversionMath.ToBaseAsync(
                db, key, order.TargetQuantity, order.UomId, ct);
            if (!okBase)
                return (false, convErr ?? "Could not convert production order quantity to the finished material base UOM.", lines);

            var factor = qtyInBase / baseQty;

            var items = await db.BomItemsSamples.AsNoTracking()
                .Where(i => i.BomID == header.BomID)
                .Include(i => i.Uom)
                .Include(i => i.CreateMaterialMaster)
                .OrderBy(i => i.ItemID)
                .ToListAsync(ct);

            foreach (var i in items)
            {
                if (string.IsNullOrWhiteSpace(i.MaterialNumber))
                    continue;

                var lineQty = (i.Quantity ?? 0m) * factor;
                lines.Add(new MrpBomLineDisplayDto
                {
                    MaterialNumber = i.MaterialNumber.Trim(),
                    Description = i.CreateMaterialMaster?.Description,
                    MaterialTypeCode = i.CreateMaterialMaster?.MaterialTypeCode,
                    Quantity = decimal.Round(lineQty, 6, MidpointRounding.AwayFromZero),
                    UomCode = i.Uom?.Code
                });
            }

            return (true, null, lines);
        }

        /// <summary>
        /// BOM lines for the PO BOM modal: uses <see cref="ProductionOrder.ReleasedBomSnapshotJson"/> when present,
        /// otherwise the live BOM scaled to order quantity.
        /// </summary>
        public static async Task<(bool success, string? error, List<MrpBomLineDisplayDto> lines)> GetBomLinesScaledForProductionOrderAsync(
            AppDbContext db,
            ProductionOrder order,
            CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(order.ReleasedBomSnapshotJson))
            {
                try
                {
                    var frozen = JsonSerializer.Deserialize<List<MrpBomLineDisplayDto>>(order.ReleasedBomSnapshotJson, BomSnapshotReadOptions);
                    return (true, null, frozen ?? new List<MrpBomLineDisplayDto>());
                }
                catch
                {
                    return (false, "Stored production order BOM snapshot could not be read.", new List<MrpBomLineDisplayDto>());
                }
            }

            return await BuildScaledBomLinesFromLiveBomAsync(db, order, ct);
        }

        public static string SerializeBomSnapshotLines(IReadOnlyList<MrpBomLineDisplayDto> lines) =>
            JsonSerializer.Serialize(lines, BomSnapshotJsonOptions);
    }
}
