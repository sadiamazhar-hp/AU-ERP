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

            var exists = await _db.GoodsProduceBatches.AnyAsync(g => g.ProductionOrderId == dto.ProductionOrderId, ct)
                .ConfigureAwait(false);
            if (exists)
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new(false, "Goods receipt has already been posted for this order.");
            }

            var grDate = dto.GrDate.Date;
            var row = new GoodsProduceBatch
            {
                ProductionOrderId = dto.ProductionOrderId,
                GrDate = grDate,
                ProducedQty = dto.ProducedQty,
                QtyFirstQuality = dto.QtyFirstQuality,
                QtySecondQuality = dto.QtySecondQuality,
                QtyThirdQuality = dto.QtyThirdQuality,
                RejectedScrapQty = dto.RejectedScrapQty,
                BatchNo = batchNo,
                CreatedAt = DateTime.UtcNow
            };

            _db.GoodsProduceBatches.Add(row);
            await UpsertInventoryFromGoodsReceiptAsync(po, dto, ct).ConfigureAwait(false);
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
    private async Task UpsertInventoryFromGoodsReceiptAsync(ProductionOrder po, GoodsReceiptPostDto dto, CancellationToken ct)
    {
        var plantId = (po.ReleasedRouting?.PlantID ?? "").Trim();
        if (plantId.Length == 0)
            return;

        var mat = await _db.CreateMaterialMaster.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MaterialNumber == po.FinishedMaterialNumber, ct)
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
            (dto.QtyFirstQuality, StockInventoryGradeCodes.FirstQuality),
            (dto.QtySecondQuality, StockInventoryGradeCodes.SecondQuality),
            (dto.QtyThirdQuality, StockInventoryGradeCodes.ThirdQuality),
            (dto.RejectedScrapQty, StockInventoryGradeCodes.Scrap)
        };

        foreach (var (qty, grade) in splits)
        {
            if (qty <= 0)
                continue;

            var stdCost = await InventoryStandardCostService.ResolveStandardCostPerUomAsync(
                    _db,
                    po.FinishedMaterialNumber,
                    po.UomId,
                    ct,
                    grade,
                    plantId)
                .ConfigureAwait(false);

            var line = await _db.StockInventoryLines
                .FirstOrDefaultAsync(
                    s => s.PlantID == plantId
                         && s.MaterialNumber == po.FinishedMaterialNumber
                         && s.QuantityUomId == po.UomId
                         && s.Status == status
                         && s.Grade == grade,
                    ct)
                .ConfigureAwait(false);

            if (line == null)
            {
                _db.StockInventoryLines.Add(new StockInventoryLine
                {
                    MaterialNumber = po.FinishedMaterialNumber,
                    PlantID = plantId,
                    QuantityUomId = po.UomId,
                    Status = status,
                    Grade = grade,
                    Quantity = qty,
                    StandardCostPerUom = stdCost,
                    StockValue = Math.Round(qty * stdCost, 2, MidpointRounding.AwayFromZero),
                    BatchOrLot = (dto.BatchNo ?? "").Trim().Length == 0 ? null : (dto.BatchNo ?? "").Trim(),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                line.Quantity += qty;
                line.StockValue = Math.Round(line.Quantity * line.StandardCostPerUom, 2, MidpointRounding.AwayFromZero);
                if (string.IsNullOrWhiteSpace(line.BatchOrLot) && !string.IsNullOrWhiteSpace(dto.BatchNo))
                    line.BatchOrLot = dto.BatchNo.Trim();
                line.UpdatedAt = now;
            }
        }
    }
}

