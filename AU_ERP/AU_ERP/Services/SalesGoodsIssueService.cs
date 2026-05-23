using AU_ERP.Configuration;
using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

public sealed class SalesGoodsIssueService
{
    private readonly AppDbContext _db;
    private readonly DocumentNumberAllocator _documentNumbers;
    private const decimal InventoryEpsilon = 0.0001m;

    public SalesGoodsIssueService(AppDbContext db, DocumentNumberAllocator documentNumbers)
    {
        _db = db;
        _documentNumbers = documentNumbers;
    }

    public async Task<(bool success, string message, int? salesGoodsIssueId)> CreateOrOpenPendingAsync(
        int salesOrderId,
        string? userId,
        CancellationToken ct = default)
    {
        if (salesOrderId <= 0)
            return (false, "Invalid sales order.", null);

        var existing = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .Where(g => g.SalesOrderId == salesOrderId)
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync(ct);
        if (existing != null)
            return (true, "Sales goods issue already exists.", existing.Id);

        var so = await _db.SalesOrders.AsNoTracking()
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == salesOrderId, ct);
        if (so == null)
            return (false, "Sales order not found.", null);
        if (!string.Equals(so.Status, SalesOrder.StatusConfirmed, StringComparison.OrdinalIgnoreCase))
            return (false, "Goods issue can only be created for confirmed sales orders.", null);
        if (await _db.DeliveryChallans.AsNoTracking().AnyAsync(d => d.SalesOrderId == salesOrderId, ct))
            return (false, "Delivery challan already exists for this sales order.", null);

        var lines = so.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.MaterialNumber) && i.OrderQuantity > 0 && i.QuantityUomId is > 0)
            .OrderBy(i => i.Id)
            .ToList();
        if (lines.Count == 0)
            return (false, "Sales order has no valid lines to issue.", null);

        string number;
        try
        {
            number = await _documentNumbers.AllocateAsync(ModuleKeys.StockGoodsIssue, ct).ConfigureAwait(false);
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
        var doc = new SalesGoodsIssueDocument
        {
            SalesOrderId = so.Id,
            DocumentNumber = number,
            DocumentDate = DateTime.Today,
            Status = SalesGoodsIssueDocument.StatusPending,
            DispatchStatus = SalesGoodsIssueDocument.DispatchPending,
            CreatedAt = now,
            CreatedByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim()
        };

        foreach (var ln in lines)
        {
            var qty = decimal.Round(ln.OrderQuantity, 4, MidpointRounding.AwayFromZero);
            doc.Lines.Add(new SalesGoodsIssueDocumentLine
            {
                SalesOrderItemId = ln.Id,
                MaterialNumber = ln.MaterialNumber.Trim(),
                MaterialDescription = ln.MaterialDescription,
                SalesPriceGrade = ln.SalesPriceGrade,
                RequiredQty = qty,
                IssuedQty = 0m,
                RemainingQty = qty,
                RequiredUomId = ln.QuantityUomId!.Value
            });
        }

        _db.SalesGoodsIssueDocuments.Add(doc);
        await _db.SaveChangesAsync(ct);
        return (true, "Sales goods issue created.", doc.Id);
    }

    public async Task<(bool success, string message)> SendGoodsAsync(
        int salesGoodsIssueId,
        string? userId,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var doc = await _db.SalesGoodsIssueDocuments
                .Include(g => g.Lines)
                .FirstOrDefaultAsync(g => g.Id == salesGoodsIssueId, ct);
            if (doc == null)
            {
                await tx.RollbackAsync(ct);
                return (false, "Sales goods issue not found.");
            }

            if (string.Equals(doc.Status, SalesGoodsIssueDocument.StatusReceived, StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return (false, "Sales goods issue is already received.");
            }

            if (!string.Equals(doc.DispatchStatus, SalesGoodsIssueDocument.DispatchPending, StringComparison.OrdinalIgnoreCase))
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

            var soForPlant = await _db.SalesOrders.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == doc.SalesOrderId, ct)
                .ConfigureAwait(false);
            if (soForPlant == null)
            {
                await tx.RollbackAsync(ct);
                return (false, "Linked sales order not found.");
            }
            var plantId = (soForPlant.PlantId ?? "").Trim();
            if (plantId.Length == 0)
            {
                await tx.RollbackAsync(ct);
                return (false, "Sales order plant is required to deduct inventory on dispatch.");
            }

            if (!doc.Lines.Any(l => l.RemainingQty > InventoryEpsilon))
            {
                await tx.RollbackAsync(ct);
                return (false, "No quantities left to dispatch on this goods issue.");
            }

            var dispatchErr = await DispatchRemainingLinesAsync(doc, plantId, ct);
            if (dispatchErr != null)
            {
                await tx.RollbackAsync(ct);
                return (false, dispatchErr);
            }

            var now = DateTime.UtcNow;
            doc.DispatchStatus = SalesGoodsIssueDocument.DispatchSent;
            doc.DispatchSentAt = now;
            doc.DispatchSentByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, "Sales goods issue marked as sent. Inventory has been deducted and batch information recorded.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    public async Task<(bool success, string message)> ReceiveGoodsAsync(
        int salesGoodsIssueId,
        string? userId,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var doc = await _db.SalesGoodsIssueDocuments
                .Include(g => g.SalesOrder)
                .Include(g => g.Lines)
                .FirstOrDefaultAsync(g => g.Id == salesGoodsIssueId, ct);
            if (doc == null)
            {
                await tx.RollbackAsync(ct);
                return (false, "Sales goods issue not found.");
            }
            if (string.Equals(doc.Status, SalesGoodsIssueDocument.StatusReceived, StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return (false, "Sales goods issue is already received.");
            }

            if (!string.Equals(doc.DispatchStatus, SalesGoodsIssueDocument.DispatchSent, StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return (false, "Goods receive is only allowed after dispatch has been sent from Stock Goods Issue.");
            }

            if (doc.SalesOrder == null)
            {
                await tx.RollbackAsync(ct);
                return (false, "Linked sales order not found.");
            }
            if (!string.Equals(doc.SalesOrder.Status, SalesOrder.StatusConfirmed, StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return (false, "Linked sales order is not confirmed.");
            }

            var hadOutstandingDispatch = doc.Lines.Any(l => l.RemainingQty > InventoryEpsilon);
            if (hadOutstandingDispatch)
            {
                var plantId = (doc.SalesOrder.PlantId ?? "").Trim();
                if (plantId.Length == 0)
                {
                    await tx.RollbackAsync(ct);
                    return (false, "Sales order plant is required to deduct inventory on goods receive.");
                }

                var dispatchErr = await DispatchRemainingLinesAsync(doc, plantId, ct);
                if (dispatchErr != null)
                {
                    await tx.RollbackAsync(ct);
                    return (false, dispatchErr);
                }
            }

            doc.Status = SalesGoodsIssueDocument.StatusReceived;
            doc.ReceivedAt = DateTime.UtcNow;
            doc.ReceivedByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, hadOutstandingDispatch
                ? "Sales goods issue received. Inventory deducted for outstanding dispatch quantities."
                : "Sales goods issue received. (Stock was already deducted when goods were sent.)");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    private async Task<string?> DispatchRemainingLinesAsync(
        SalesGoodsIssueDocument doc,
        string plantId,
        CancellationToken ct)
    {
        var reqLines = doc.Lines
            .Where(l => !string.IsNullOrWhiteSpace(l.MaterialNumber) && l.RequiredUomId > 0 && l.RemainingQty > InventoryEpsilon)
            .ToList();
        if (reqLines.Count == 0)
            return null;

        var gradeBySoItem = reqLines
            .Where(l => l.SalesOrderItemId is > 0)
            .ToDictionary(l => l.SalesOrderItemId!.Value, l => l.SalesPriceGrade);

        var deducts = reqLines.Select(l => new DeliveryChallanStockService.LineDeduct
        {
            MaterialNumber = l.MaterialNumber.Trim(),
            Qty = l.RemainingQty,
            QuantityUomId = l.RequiredUomId,
            SalesOrderItemId = l.SalesOrderItemId
        }).ToList();

        var (okDeduct, errDeduct, batches) = await DeliveryChallanStockService.DeductAndGetBatchLabelsAsync(
            _db,
            deducts,
            gradeBySoItem,
            plantId,
            ct);
        if (!okDeduct)
            return errDeduct ?? "Could not deduct stock when dispatching goods.";

        for (var i = 0; i < reqLines.Count; i++)
        {
            var line = reqLines[i];
            var issuedNow = line.RemainingQty;
            line.IssuedQty = decimal.Round(line.IssuedQty + issuedNow, 4, MidpointRounding.AwayFromZero);
            line.RemainingQty = decimal.Round(Math.Max(0m, line.RequiredQty - line.IssuedQty), 4, MidpointRounding.AwayFromZero);
            var b = (batches != null && i < batches.Count) ? batches[i] : null;
            line.BatchSummary = string.IsNullOrWhiteSpace(b) ? null : b;
        }

        if (doc.Lines.Any(l => l.RemainingQty > InventoryEpsilon))
            return "Could not fully dispatch all sales goods issue lines against stock.";

        return null;
    }

    public async Task<(bool success, string message, int? salesGoodsIssueId)> ReceiveBySalesOrderAsync(
        int salesOrderId,
        string? userId,
        CancellationToken ct = default)
    {
        if (salesOrderId <= 0)
            return (false, "Invalid sales order.", null);
        var doc = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .Where(g => g.SalesOrderId == salesOrderId)
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync(ct);
        if (doc == null)
            return (false, "Sales goods issue not found for this sales order.", null);
        var (ok, msg) = await ReceiveGoodsAsync(doc.Id, userId, ct);
        return (ok, msg, doc.Id);
    }

}
