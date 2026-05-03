using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

public sealed class GoodsReceiptPostingService
{
    private readonly AppDbContext _db;

    public GoodsReceiptPostingService(AppDbContext db)
    {
        _db = db;
    }

    public sealed record PostResult(bool Success, string Message);

    public async Task<PostResult> PostAsync(GoodsReceiptPostDto? dto, CancellationToken ct = default)
    {
        if (dto == null || dto.ProductionOrderId <= 0)
            return new(false, "Invalid request.");

        var batchNo = (dto.BatchNo ?? "").Trim();
        if (string.IsNullOrEmpty(batchNo) || batchNo.Length > 64)
            return new(false, "Batch number is required (max 64 characters).");

        if (dto.ProducedQty < 0 || dto.QtyFirstQuality < 0 || dto.QtySecondQuality < 0
            || dto.QtyThirdQuality < 0 || dto.RejectedScrapQty < 0)
            return new(false, "Quantities cannot be negative.");

        var sum = dto.QtyFirstQuality + dto.QtySecondQuality + dto.QtyThirdQuality + dto.RejectedScrapQty;
        if (Math.Abs(dto.ProducedQty - sum) > 0.0001m)
            return new(false, "Produced quantity must equal first + second + third quality + rejected/scrap.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var po = await _db.ProductionOrders
                .Include(p => p.Lines)
                .Include(p => p.StageProgresses)
                .Include(p => p.ReleasedRouting)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductionOrderId, ct)
                .ConfigureAwait(false);

            if (po == null)
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new(false, "Production order not found.");
            }

            if (po.ReleasedRoutingId == null)
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new(false, "Only released production orders can post goods receipt.");
            }

            if (po.ReleasedRouting == null || string.IsNullOrWhiteSpace(po.ReleasedRouting.PlantID))
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new(false, "Released routing must have a plant configured to post inventory.");
            }

            var stages = po.StageProgresses.OrderBy(s => s.SequenceOrder).ToList();
            if (stages.Count == 0 || stages.Any(s => s.StageStatus != ProductionOrderStageProgress.StageCompleted))
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new(false, "All operation stages must be completed before posting goods receipt.");
            }

            var lines = BuildPostingLines(po, dto);
            if (lines.Count == 0)
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new(false, "At least one goods receipt line is required.");
            }
            
            var producedByLine = await _db.GoodsProduceBatches.AsNoTracking()
                .Where(g => g.ProductionOrderId == po.Id)
                .GroupBy(g => g.ProductionOrderLineId ?? 0)
                .Select(g => new { LineId = g.Key, Qty = g.Sum(x => x.ProducedQty) })
                .ToDictionaryAsync(x => x.LineId, x => x.Qty, ct)
                .ConfigureAwait(false);
            
            var poLines = po.Lines.OrderBy(l => l.LineNo).ToList();
            if (poLines.Count == 0)
            {
                poLines.Add(new ProductionOrderLine
                {
                    Id = 0,
                    ProductionOrderId = po.Id,
                    LineNo = 1,
                    MaterialNumber = po.FinishedMaterialNumber,
                    PlannedQuantity = po.TargetQuantity,
                    UomId = po.UomId
                });
            }
            
            foreach (var ln in lines)
            {
                var poLine = poLines.FirstOrDefault(x => x.Id == ln.ProductionOrderLineId)
                             ?? poLines.FirstOrDefault(x => x.MaterialNumber == ln.MaterialNumber && x.UomId == ln.UomId)
                             ?? poLines.First();
                var key = poLine.Id;
                var already = producedByLine.TryGetValue(key, out var aq) ? aq : 0m;
                if (already + ln.ProducedQty > poLine.PlannedQuantity + 0.0001m)
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    return new(false, $"Goods receipt exceeds planned quantity for material '{poLine.MaterialNumber}'. Planned {poLine.PlannedQuantity:0.####}, already received {already:0.####}, this post {ln.ProducedQty:0.####}.");
                }
            }

            var grDate = dto.GrDate.Date;
            foreach (var ln in lines)
            {
                var row = new GoodsProduceBatch
                {
                    ProductionOrderId = dto.ProductionOrderId,
                    ProductionOrderLineId = ln.ProductionOrderLineId,
                    MaterialNumber = ln.MaterialNumber,
                    UomId = ln.UomId,
                    GrDate = grDate,
                    ProducedQty = ln.ProducedQty,
                    QtyFirstQuality = ln.QtyFirstQuality,
                    QtySecondQuality = ln.QtySecondQuality,
                    QtyThirdQuality = ln.QtyThirdQuality,
                    RejectedScrapQty = ln.RejectedScrapQty,
                    BatchNo = batchNo,
                    CreatedAt = DateTime.UtcNow
                };
                _db.GoodsProduceBatches.Add(row);
                await UpsertInventoryFromGoodsReceiptAsync(po, ln, dto.BatchNo, ct).ConfigureAwait(false);
            }
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);

            return new(true, "Goods receipt posted. Inventory updated for finished / semi-finished material.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            return new(false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    /// <summary>
    /// For FERT/HALB (finished / semi-finished), add GR quantities into stock by grade (A/B/C/Scrap).
    /// Merges into an existing line when material + UOM + status + grade already exist.
    /// </summary>
    private async Task UpsertInventoryFromGoodsReceiptAsync(
        ProductionOrder po,
        GoodsReceiptPostLineDto lineDto,
        string? batchNo,
        CancellationToken ct)
    {
        var plantId = (po.ReleasedRouting?.PlantID ?? "").Trim();
        if (plantId.Length == 0)
            return;

        var mat = await _db.CreateMaterialMaster.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MaterialNumber == lineDto.MaterialNumber, ct)
            .ConfigureAwait(false);
        if (mat == null)
            return;

        var mt = (mat.MaterialTypeCode ?? "").Trim().ToUpperInvariant();
        if (mt != "FERT" && mt != "HALB")
            return;

        var now = DateTime.UtcNow;
        var status = StockInventoryLine.StatusActive;
        var splits = new[]
        {
            (lineDto.QtyFirstQuality, StockInventoryGradeCodes.FirstQuality),
            (lineDto.QtySecondQuality, StockInventoryGradeCodes.SecondQuality),
            (lineDto.QtyThirdQuality, StockInventoryGradeCodes.ThirdQuality),
            (lineDto.RejectedScrapQty, StockInventoryGradeCodes.Scrap)
        };

        foreach (var (qty, grade) in splits)
        {
            if (qty <= 0)
                continue;

            var stdCost = await InventoryStandardCostService.ResolveStandardCostPerUomAsync(
                    _db,
                    lineDto.MaterialNumber,
                    lineDto.UomId,
                    ct,
                    grade,
                    plantId)
                .ConfigureAwait(false);

            var batchKey = StockInventoryBatchKey.Normalize(batchNo);
            var line = await _db.StockInventoryLines
                .FirstOrDefaultAsync(
                    s => s.PlantID == plantId
                         && s.MaterialNumber == lineDto.MaterialNumber
                         && s.QuantityUomId == lineDto.UomId
                         && s.Status == status
                         && s.Grade == grade
                         && (s.BatchOrLot ?? "") == batchKey,
                    ct)
                .ConfigureAwait(false);

            if (line == null)
            {
                _db.StockInventoryLines.Add(new StockInventoryLine
                {
                    MaterialNumber = lineDto.MaterialNumber,
                    PlantID = plantId,
                    QuantityUomId = lineDto.UomId,
                    Status = status,
                    Grade = grade,
                    Quantity = qty,
                    StandardCostPerUom = stdCost,
                    StockValue = Math.Round(qty * stdCost, 2, MidpointRounding.AwayFromZero),
                    BatchOrLot = batchKey.Length == 0 ? "" : batchKey,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                line.Quantity += qty;
                line.StockValue = Math.Round(line.Quantity * line.StandardCostPerUom, 2, MidpointRounding.AwayFromZero);
                if (string.IsNullOrWhiteSpace(line.BatchOrLot) && batchKey.Length > 0)
                    line.BatchOrLot = batchKey;
                line.UpdatedAt = now;
            }
        }
    }

    private static List<GoodsReceiptPostLineDto> BuildPostingLines(ProductionOrder po, GoodsReceiptPostDto dto)
    {
        if (dto.Lines != null && dto.Lines.Count > 0)
            return dto.Lines.Where(x => x.ProducedQty > 0 && !string.IsNullOrWhiteSpace(x.MaterialNumber)).ToList();
        return new List<GoodsReceiptPostLineDto>
        {
            new()
            {
                ProductionOrderLineId = po.Lines.OrderBy(l => l.LineNo).Select(l => (int?)l.Id).FirstOrDefault(),
                MaterialNumber = po.FinishedMaterialNumber,
                UomId = po.UomId,
                ProducedQty = dto.ProducedQty,
                QtyFirstQuality = dto.QtyFirstQuality,
                QtySecondQuality = dto.QtySecondQuality,
                QtyThirdQuality = dto.QtyThirdQuality,
                RejectedScrapQty = dto.RejectedScrapQty
            }
        };
    }
}

