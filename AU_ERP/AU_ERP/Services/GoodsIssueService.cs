using AU_ERP.Configuration;
using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

public sealed class GoodsIssueService
{
    private readonly AppDbContext _db;
    private readonly DocumentNumberAllocator _documentNumbers;
    private const decimal InventoryEpsilon = 0.0001m;

    public GoodsIssueService(AppDbContext db, DocumentNumberAllocator documentNumbers)
    {
        _db = db;
        _documentNumbers = documentNumbers;
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
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == productionOrderId, ct);
        if (po == null)
            return (false, "Production order not found.", null);
        if (po.Status != ProductionOrder.StatusPlanned)
            return (false, "Goods issue can only be created for planned production orders.", null);
        if (po.ReleasedRoutingId != null)
            return (false, "This production order is already released to operations.", null);

        var poLines = po.Lines.OrderBy(l => l.LineNo).ToList();
        if (poLines.Count == 0)
        {
            poLines.Add(new ProductionOrderLine
            {
                ProductionOrderId = po.Id,
                LineNo = 1,
                MaterialNumber = po.FinishedMaterialNumber,
                PlannedQuantity = po.TargetQuantity,
                UomId = po.UomId
            });
        }

        string docNumber;
        try
        {
            docNumber = await _documentNumbers.AllocateAsync(ModuleKeys.StockGoodsIssue, ct).ConfigureAwait(false);
        }
        catch (DocumentIntegrationMissingException ex)
        {
            return (false, ex.Message, null);
        }
        catch (DocumentIntegrationRangeExhaustedException ex)
        {
            return (false, ex.Message, null);
        }

        var now = DateTime.UtcNow;
        var doc = new GoodsIssueDocument
        {
            ProductionOrderId = po.Id,
            DocumentDate = DateTime.Today,
            DocumentNumber = docNumber,
            Status = GoodsIssueDocument.StatusPending,
            DispatchStatus = GoodsIssueDocument.DispatchPending,
            CreatedAt = now,
            CreatedByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim()
        };

        foreach (var line in poLines)
        {
            var routing = await ResolveRoutingAsync(line.MaterialNumber, ct);
            if (routing == null || routing.OperationHeaders == null || !routing.OperationHeaders.Any())
                return (false, $"No routing with operations is defined for line {line.LineNo} material '{line.MaterialNumber}'.", null);
            if (string.IsNullOrWhiteSpace(routing.PlantID))
                return (false, $"Routing plant is missing for line {line.LineNo} material '{line.MaterialNumber}'.", null);
            
            var mrp = await MrpExplosionService.RunAsync(
                _db,
                line.MaterialNumber.Trim(),
                line.PlannedQuantity,
                line.UomId,
                requireFertMaterialOnly: false,
                selectedBomId: line.SelectedBomId,
                plantIdForStockOverride: line.PlantId ?? routing.PlantID,
                ct);
            if (!mrp.Success)
                return (false, $"Line {line.LineNo}: {mrp.Message ?? "MRP check failed for goods issue creation."}", null);
            if (mrp.Rows == null || mrp.Rows.Count == 0)
                return (false, $"No BOM components found for line {line.LineNo} material '{line.MaterialNumber}'.", null);
            
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
            
            foreach (var row in grouped)
            {
                var required = decimal.Round(row.RequiredQty, 4, MidpointRounding.AwayFromZero);
                doc.Lines.Add(new GoodsIssueDocumentLine
                {
                    ProductionOrderLineId = line.Id > 0 ? line.Id : null,
                    FertMaterialNumber = line.MaterialNumber,
                    FertMaterialDescription = line.MaterialDescription,
                    MaterialNumber = row.MaterialNumber,
                    MaterialDescription = row.Description,
                    RequiredQty = required,
                    IssuedQty = 0m,
                    RemainingQty = required,
                    RequiredUomId = row.RequiredUomId,
                    SelectedBomId = line.SelectedBomId,
                    SelectedBomAlternative = line.SelectedBomAlternative
                });
            }
        }

        _db.GoodsIssueDocuments.Add(doc);
        await _db.SaveChangesAsync(ct);
        return (true, "Goods issue created.", doc.Id);
    }

    /// <summary>Marks dispatch as sent after validating lines (no stock movement).</summary>
    public async Task<(bool success, string message)> SendGoodsAsync(
        int goodsIssueId,
        string? userId,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var doc = await _db.GoodsIssueDocuments
                .Include(g => g.Lines)
                .FirstOrDefaultAsync(g => g.Id == goodsIssueId, ct);
            if (doc == null)
            {
                await tx.RollbackAsync(ct);
                return (false, "Goods issue document not found.");
            }

            if (string.Equals(doc.Status, GoodsIssueDocument.StatusCompleted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(doc.Status, GoodsIssueDocument.StatusReceived, StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return (false, string.Equals(doc.Status, GoodsIssueDocument.StatusReceived, StringComparison.OrdinalIgnoreCase)
                    ? "Dispatch cannot be changed after reservation goods receive."
                    : "Goods issue is already completed.");
            }

            if (!string.Equals(doc.DispatchStatus, GoodsIssueDocument.DispatchPending, StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return (false, "Send Goods is only available while dispatch is pending.");
            }

            if (doc.Lines == null || doc.Lines.Count == 0)
            {
                await tx.RollbackAsync(ct);
                return (false, "Cannot send: no materials on this goods issue.");
            }

            foreach (var line in doc.Lines)
            {
                if (string.IsNullOrWhiteSpace(line.MaterialNumber) || line.RequiredUomId <= 0 || line.RequiredQty <= 0)
                {
                    await tx.RollbackAsync(ct);
                    return (false, "Cannot send: all lines must have material, UOM, and a positive required quantity.");
                }
            }

            var now = DateTime.UtcNow;
            doc.DispatchStatus = GoodsIssueDocument.DispatchSent;
            doc.DispatchSentAt = now;
            doc.DispatchSentByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, "Goods issue marked as sent. Receipt is now allowed on the source document.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    public async Task<(bool success, string message)> ReceiveGoodsAsync(
        int goodsIssueId,
        string? userId,
        IReadOnlyDictionary<int, decimal>? requestedIssueQtyByLine = null,
        CancellationToken ct = default)
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

            if (string.Equals(doc.Status, GoodsIssueDocument.StatusCompleted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(doc.Status, GoodsIssueDocument.StatusReceived, StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return (false, string.Equals(doc.Status, GoodsIssueDocument.StatusReceived, StringComparison.OrdinalIgnoreCase)
                    ? "Reservation goods receive is already finished. Use Release on the production order to start operations."
                    : "Goods issue is already completed.");
            }

            if (!string.Equals(doc.DispatchStatus, GoodsIssueDocument.DispatchSent, StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return (false, "Receive goods is only allowed after dispatch has been sent from Stock Goods Issue.");
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
                return (false, "Production order is not in a state that allows reservation goods receive.");
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

            var consumeErr = await ConsumeInventoryAsync(po, doc.Lines.ToList(), requestedIssueQtyByLine, ct);
            if (consumeErr != null)
            {
                await tx.RollbackAsync(ct);
                return (false, consumeErr);
            }
            
            var allIssued = doc.Lines.All(l => l.RemainingQty <= InventoryEpsilon);
            if (allIssued)
            {
                doc.Status = GoodsIssueDocument.StatusReceived;
                doc.CompletedAt = null;
                doc.CompletedByUserId = null;
                po.Status = ProductionOrder.StatusPlanned;
            }
            else
            {
                doc.Status = GoodsIssueDocument.StatusPending;
                po.Status = ProductionOrder.StatusPlanned;
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return allIssued
                ? (true, "Reservation goods receive completed. Inventory deducted. Release the production order to start operations.")
                : (true, "Partial goods issue posted. Remaining quantities are still pending.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    /// <summary>After full reservation goods receive, creates routing stage progress and marks the production order in progress.</summary>
    public async Task<(bool success, string message)> ReleaseReservationToOperationsAsync(
        int goodsIssueId,
        string? userId,
        CancellationToken ct = default)
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

            if (!string.Equals(doc.Status, GoodsIssueDocument.StatusReceived, StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return (false, "Release is only available after reservation goods receive has fully posted from stock.");
            }

            if (!string.Equals(doc.DispatchStatus, GoodsIssueDocument.DispatchSent, StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return (false, "Dispatch must be sent from Stock Goods Issue before release.");
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
                return (false, "Production order is not in a state that can be released to operations.");
            }

            if (doc.Lines == null || doc.Lines.Count == 0
                || !doc.Lines.All(l => l.RemainingQty <= InventoryEpsilon))
            {
                await tx.RollbackAsync(ct);
                return (false, "Complete reservation goods receive before releasing to operations.");
            }

            var now = DateTime.UtcNow;
            var releaseErr = await ApplyReleaseProductionOrderToOperationsAsync(po, now, ct);
            if (releaseErr != null)
            {
                await tx.RollbackAsync(ct);
                return (false, releaseErr);
            }

            doc.Status = GoodsIssueDocument.StatusCompleted;
            doc.CompletedAt = now;
            doc.CompletedByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, "Production order released to operations.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    private async Task<string?> ApplyReleaseProductionOrderToOperationsAsync(
        ProductionOrder po,
        DateTime now,
        CancellationToken ct)
    {
        var poLines = await _db.ProductionOrderLines
            .Where(l => l.ProductionOrderId == po.Id)
            .OrderBy(l => l.LineNo)
            .ToListAsync(ct);
        if (poLines.Count == 0)
        {
            poLines.Add(new ProductionOrderLine
            {
                ProductionOrderId = po.Id,
                LineNo = 1,
                MaterialNumber = po.FinishedMaterialNumber,
                PlannedQuantity = po.TargetQuantity,
                UomId = po.UomId
            });
        }

        var allBomLines = new List<MrpBomLineDisplayDto>();
        int? firstRoutingId = po.ReleasedRoutingId;
        foreach (var line in poLines)
        {
            var routing = await ResolveRoutingAsync(line.MaterialNumber, ct);
            if (routing == null || routing.OperationHeaders == null || !routing.OperationHeaders.Any())
                return $"No routing with operations is defined for line {line.LineNo} material '{line.MaterialNumber}'.";
            if (firstRoutingId == null)
                firstRoutingId = routing.RoutingID;
            var orderedHeaders = routing.OperationHeaders.OrderBy(h => h.DisplayOrder).ToList();
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
                    return ex.Message;
                }

                await _db.ProductionOrderStageProgresses.AddAsync(new ProductionOrderStageProgress
                {
                    ProductionOrderId = po.Id,
                    ProductionOrderLineId = line.Id > 0 ? line.Id : null,
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

            var poLike = new ProductionOrder
            {
                FinishedMaterialNumber = line.MaterialNumber,
                TargetQuantity = (int)Math.Round(line.PlannedQuantity, MidpointRounding.AwayFromZero),
                UomId = line.UomId,
                ReleasedBomSnapshotJson = po.ReleasedBomSnapshotJson,
                SelectedBomId = line.SelectedBomId,
                SelectedBomAlternative = line.SelectedBomAlternative
            };
            var (bomOk, bomErr, bomLines) = await MrpExplosionService.GetBomLinesScaledForProductionOrderAsync(_db, poLike, ct);
            if (!bomOk)
                return bomErr ?? "Could not build BOM snapshot.";
            allBomLines.AddRange(bomLines);
        }

        po.ReleasedRoutingId = firstRoutingId;
        po.ReleasedBomSnapshotJson = MrpExplosionService.SerializeBomSnapshotLines(allBomLines);
        po.Status = ProductionOrder.StatusInProgress;
        return null;
    }

    private async Task<string?> ConsumeInventoryAsync(
        ProductionOrder po,
        IReadOnlyList<GoodsIssueDocumentLine> lines,
        IReadOnlyDictionary<int, decimal>? requestedIssueQtyByLine,
        CancellationToken ct)
    {
        var reqLines = lines
            .Where(l => !string.IsNullOrWhiteSpace(l.MaterialNumber) && l.RequiredUomId > 0 && l.RemainingQty > 0)
            .ToList();

        foreach (var req in reqLines)
        {
            var issueQtyRequested = req.RemainingQty;
            if (requestedIssueQtyByLine != null && requestedIssueQtyByLine.TryGetValue(req.Id, out var reqQty) && reqQty >= 0)
                issueQtyRequested = Math.Min(req.RemainingQty, reqQty);
            if (issueQtyRequested <= 0)
                continue;
            
            var plantId = await ResolveIssuePlantForLineAsync(po, req, ct);
            if (string.IsNullOrWhiteSpace(plantId))
                return $"Cannot receive goods: routing plant is missing for line material '{req.FertMaterialNumber ?? po.FinishedMaterialNumber}'.";
            
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

            if (available + InventoryEpsilon < issueQtyRequested)
            {
                return $"Cannot receive goods: insufficient stock for '{req.MaterialNumber}' at plant '{plantId}'. Required {issueQtyRequested:0.####}, available {available:0.####}.";
            }

            decimal remaining = issueQtyRequested;
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
            
            var issuedNow = issueQtyRequested - remaining;
            req.IssuedQty = decimal.Round(req.IssuedQty + issuedNow, 4, MidpointRounding.AwayFromZero);
            req.RemainingQty = decimal.Round(Math.Max(0m, req.RequiredQty - req.IssuedQty), 4, MidpointRounding.AwayFromZero);
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

    private async Task<string?> ResolveIssuePlantForLineAsync(
        ProductionOrder po,
        GoodsIssueDocumentLine line,
        CancellationToken ct)
    {
        var mat = (line.FertMaterialNumber ?? po.FinishedMaterialNumber ?? "").Trim();
        if (mat.Length == 0)
            return null;
        var routing = await ResolveRoutingAsync(mat, ct);
        return routing?.PlantID;
    }

}
