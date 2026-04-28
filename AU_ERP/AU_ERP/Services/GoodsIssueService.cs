using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

public sealed class GoodsIssueService
{
    private readonly AppDbContext _db;
    private const decimal InventoryEpsilon = 0.0001m;

    public GoodsIssueService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool success, string message, int? goodsIssueId)> CreateOrOpenPendingAsync(
        int productionOrderId,
        string? userId,
        CancellationToken ct = default)
    {
        if (productionOrderId <= 0)
            return (false, "Invalid production order.", null);

        var existing = await _db.GoodsIssueDocuments.AsNoTracking()
            .FirstOrDefaultAsync(g => g.ProductionOrderId == productionOrderId, ct);
        if (existing != null)
            return (true, "Goods issue already exists.", existing.Id);

        var po = await _db.ProductionOrders.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productionOrderId, ct);
        if (po == null)
            return (false, "Production order not found.", null);
        if (po.Status != ProductionOrder.StatusPlanned)
            return (false, "Goods issue can only be created for planned production orders.", null);
        if (po.ReleasedRoutingId != null)
            return (false, "This production order is already released to operations.", null);

        var routing = await ResolveRoutingAsync(po.FinishedMaterialNumber, ct);
        if (routing == null || routing.OperationHeaders == null || !routing.OperationHeaders.Any())
            return (false, "No routing with operations is defined for this material.", null);
        if (string.IsNullOrWhiteSpace(routing.PlantID))
            return (false, "Routing must have a plant for goods issue.", null);

        var mrp = await MrpExplosionService.RunAsync(
            _db,
            po.FinishedMaterialNumber.Trim(),
            po.TargetQuantity,
            po.UomId,
            requireFertMaterialOnly: false,
            plantIdForStockOverride: routing.PlantID,
            ct);
        if (!mrp.Success)
            return (false, mrp.Message ?? "MRP check failed for goods issue creation.", null);
        if (mrp.Rows == null || mrp.Rows.Count == 0)
            return (false, "No BOM components found for this production order.", null);

        var docNumber = await GenerateNextGoodsIssueNumberAsync(ct);
        var now = DateTime.UtcNow;
        var doc = new GoodsIssueDocument
        {
            ProductionOrderId = po.Id,
            DocumentDate = DateTime.Today,
            DocumentNumber = docNumber,
            Status = GoodsIssueDocument.StatusPending,
            CreatedAt = now,
            CreatedByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim()
        };

        var grouped = mrp.Rows
            .Where(r => !string.IsNullOrWhiteSpace(r.MaterialNumber) && r.RequiredQty > 0 && r.RequiredUomId > 0)
            .GroupBy(r => new { Mat = r.MaterialNumber.Trim(), r.RequiredUomId })
            .Select(g => new
            {
                MaterialNumber = g.Key.Mat,
                RequiredUomId = g.Key.RequiredUomId,
                RequiredQty = g.Sum(x => x.RequiredQty),
                Description = g.Select(x => x.Description).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            })
            .ToList();

        if (grouped.Count == 0)
            return (false, "No BOM components found for this production order.", null);

        foreach (var row in grouped)
        {
            doc.Lines.Add(new GoodsIssueDocumentLine
            {
                MaterialNumber = row.MaterialNumber,
                MaterialDescription = row.Description,
                RequiredQty = decimal.Round(row.RequiredQty, 4, MidpointRounding.AwayFromZero),
                RequiredUomId = row.RequiredUomId
            });
        }

        _db.GoodsIssueDocuments.Add(doc);
        await _db.SaveChangesAsync(ct);
        return (true, "Goods issue created.", doc.Id);
    }

    public async Task<(bool success, string message)> ReceiveGoodsAsync(int goodsIssueId, string? userId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var doc = await _db.GoodsIssueDocuments
                .Include(g => g.ProductionOrder)
                .Include(g => g.Lines)
                .FirstOrDefaultAsync(g => g.Id == goodsIssueId, ct);
            if (doc == null)
            {
                await tx.RollbackAsync(ct);
                return (false, "Goods issue document not found.");
            }

            if (string.Equals(doc.Status, GoodsIssueDocument.StatusCompleted, StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return (false, "Goods issue is already completed.");
            }

            var po = doc.ProductionOrder;
            if (po == null)
            {
                await tx.RollbackAsync(ct);
                return (false, "Linked production order not found.");
            }

            var hasStages = await _db.ProductionOrderStageProgresses
                .AsNoTracking()
                .AnyAsync(s => s.ProductionOrderId == po.Id, ct);
            if (po.Status != ProductionOrder.StatusPlanned || po.ReleasedRoutingId != null || hasStages)
            {
                await tx.RollbackAsync(ct);
                return (false, "Production order is not in a state that can be released via goods issue.");
            }

            var routing = await ResolveRoutingAsync(po.FinishedMaterialNumber, ct);
            if (routing == null || routing.OperationHeaders == null || !routing.OperationHeaders.Any())
            {
                await tx.RollbackAsync(ct);
                return (false, "No routing with operations is defined for this material.");
            }
            if (string.IsNullOrWhiteSpace(routing.PlantID))
            {
                await tx.RollbackAsync(ct);
                return (false, "Routing must have a plant for goods issue.");
            }

            if (doc.Lines == null || doc.Lines.Count == 0)
            {
                await tx.RollbackAsync(ct);
                return (false, "No BOM items are available in this goods issue.");
            }

            foreach (var line in doc.Lines)
            {
                if (line.RequiredQty <= 0)
                {
                    await tx.RollbackAsync(ct);
                    return (false, "Goods issue lines must have positive quantity.");
                }
            }

            var consumeErr = await ConsumeInventoryAsync(routing.PlantID, doc.Lines.ToList(), ct);
            if (consumeErr != null)
            {
                await tx.RollbackAsync(ct);
                return (false, consumeErr);
            }

            var (bomOk, bomErr, bomLines) = await MrpExplosionService.BuildScaledBomLinesFromLiveBomAsync(_db, po, ct);
            if (!bomOk)
            {
                await tx.RollbackAsync(ct);
                return (false, bomErr ?? "Could not build BOM snapshot.");
            }

            var orderedHeaders = routing.OperationHeaders.OrderBy(h => h.DisplayOrder).ToList();
            var now = DateTime.UtcNow;
            var isFirst = true;
            foreach (var h in orderedHeaders)
            {
                decimal planned;
                try
                {
                    planned = RoutingPlannedHours.SumHeaderPlannedHours(h);
                }
                catch (InvalidOperationException ex)
                {
                    await tx.RollbackAsync(ct);
                    return (false, ex.Message);
                }

                await _db.ProductionOrderStageProgresses.AddAsync(new ProductionOrderStageProgress
                {
                    ProductionOrderId = po.Id,
                    RoutingOperationHeaderId = h.OperationHeaderId,
                    SequenceOrder = h.DisplayOrder,
                    StageTitle = h.Title,
                    PlannedHours = planned,
                    StageStatus = isFirst
                        ? ProductionOrderStageProgress.StageInProgress
                        : ProductionOrderStageProgress.StagePending,
                    UpdatedAt = now
                }, ct);
                isFirst = false;
            }

            po.ReleasedRoutingId = routing.RoutingID;
            po.ReleasedBomSnapshotJson = MrpExplosionService.SerializeBomSnapshotLines(bomLines);
            po.Status = ProductionOrder.StatusInProgress;

            doc.Status = GoodsIssueDocument.StatusCompleted;
            doc.CompletedAt = now;
            doc.CompletedByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, "Goods issue completed. Inventory deducted and production order released to operations.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    private async Task<string?> ConsumeInventoryAsync(string plantId, IReadOnlyList<GoodsIssueDocumentLine> lines, CancellationToken ct)
    {
        var reqLines = lines
            .Where(l => !string.IsNullOrWhiteSpace(l.MaterialNumber) && l.RequiredQty > 0 && l.RequiredUomId > 0)
            .ToList();

        foreach (var req in reqLines)
        {
            var rows = await _db.StockInventoryLines
                .Where(s => s.MaterialNumber == req.MaterialNumber
                            && s.PlantID == plantId
                            && s.Status == StockInventoryLine.StatusActive
                            && s.Quantity > 0)
                .OrderBy(s => s.UpdatedAt)
                .ThenBy(s => s.Id)
                .ToListAsync(ct);

            decimal available = 0m;
            foreach (var row in rows)
            {
                var conv = await UnitConversionMath.ConvertAsync(
                    _db,
                    req.MaterialNumber,
                    row.Quantity,
                    row.QuantityUomId,
                    req.RequiredUomId,
                    ct);
                if (conv.ok)
                    available += conv.quantityOut;
            }

            if (available + InventoryEpsilon < req.RequiredQty)
            {
                return $"Cannot receive goods: insufficient stock for '{req.MaterialNumber}' at plant '{plantId}'. Required {req.RequiredQty:0.####}, available {available:0.####}.";
            }

            decimal remaining = req.RequiredQty;
            var now = DateTime.UtcNow;
            foreach (var row in rows)
            {
                if (remaining <= InventoryEpsilon)
                    break;

                var convToReq = await UnitConversionMath.ConvertAsync(
                    _db,
                    req.MaterialNumber,
                    row.Quantity,
                    row.QuantityUomId,
                    req.RequiredUomId,
                    ct);
                if (!convToReq.ok || convToReq.quantityOut <= 0)
                    continue;

                var takeReqQty = Math.Min(convToReq.quantityOut, remaining);
                var convBack = await UnitConversionMath.ConvertAsync(
                    _db,
                    req.MaterialNumber,
                    takeReqQty,
                    req.RequiredUomId,
                    row.QuantityUomId,
                    ct);
                if (!convBack.ok || convBack.quantityOut <= 0)
                    continue;

                var takeInRowUom = Math.Min(row.Quantity, convBack.quantityOut);
                row.Quantity = Math.Round(row.Quantity - takeInRowUom, 4, MidpointRounding.AwayFromZero);
                if (row.Quantity < 0)
                    row.Quantity = 0;
                row.StockValue = Math.Round(row.Quantity * row.StandardCostPerUom, 2, MidpointRounding.AwayFromZero);
                row.UpdatedAt = now;

                var consumedReqQty = await UnitConversionMath.ConvertAsync(
                    _db,
                    req.MaterialNumber,
                    takeInRowUom,
                    row.QuantityUomId,
                    req.RequiredUomId,
                    ct);
                if (consumedReqQty.ok)
                    remaining = Math.Max(0m, remaining - consumedReqQty.quantityOut);
            }

            if (remaining > InventoryEpsilon)
                return $"Cannot receive goods: insufficient stock for '{req.MaterialNumber}' during deduction. Short by {remaining:0.####}.";
        }

        return null;
    }

    private async Task<RoutingHeadersSample?> ResolveRoutingAsync(string finishedMaterialNumber, CancellationToken ct)
    {
        var key = (finishedMaterialNumber ?? string.Empty).Trim();
        return await _db.RoutingHeadersSamples
            .Include(r => r.OperationHeaders)
            .ThenInclude(oh => oh.RoutingOperationsSamples)
            .Where(r => r.MaterialNumber == key)
            .OrderByDescending(r => r.ValidFrom)
            .ThenByDescending(r => r.RoutingID)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<string> GenerateNextGoodsIssueNumberAsync(CancellationToken ct)
    {
        const string prefix = "GI-";
        const int start = 1000;

        var numbers = await _db.GoodsIssueDocuments.AsNoTracking()
            .Select(g => g.DocumentNumber)
            .ToListAsync(ct);

        var max = 0;
        foreach (var s in numbers)
        {
            var t = (s ?? string.Empty).Trim();
            if (!t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var tail = t.Substring(prefix.Length).Trim();
            if (int.TryParse(tail, out var n))
                max = Math.Max(max, n);
        }

        var next = Math.Max(start, max + 1);
        return $"{prefix}{next}";
    }
}
