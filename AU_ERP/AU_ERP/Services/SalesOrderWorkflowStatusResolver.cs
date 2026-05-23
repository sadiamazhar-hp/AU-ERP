using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

public static class SalesOrderWorkflowStatus
{
    public const string Open = "Open";
    public const string PendingStock = "Pending Stock";
    public const string PendingGoodReceive = "Pending Good Receive";
    public const string PendingDc = "Pending DC";
    public const string DeliveryInProcess = "Delivery In Process";
    public const string PendingPayment = "Pending Payment";
    public const string Completed = "Completed";

    public static readonly IReadOnlyList<string> AllWorkflowStatuses = new[]
    {
        Open,
        PendingStock,
        PendingGoodReceive,
        PendingDc,
        DeliveryInProcess,
        PendingPayment,
        Completed
    };

    public static readonly IReadOnlyList<string> ConfirmedWorkflowStatuses = new[]
    {
        PendingStock,
        PendingGoodReceive,
        PendingDc,
        DeliveryInProcess,
        PendingPayment,
        Completed
    };
}

public sealed class SalesOrderWorkflowCountFilter
{
    public string? Q { get; init; }
    public string? PlantId { get; init; }
    public int? DistributionChannelId { get; init; }
}

public sealed class SalesOrderWorkflowStatusResolver
{
    private const decimal InventoryEpsilon = 0.0001m;
    private readonly AppDbContext _db;

    public SalesOrderWorkflowStatusResolver(AppDbContext db) => _db = db;

    public async Task<string> ResolveAsync(int salesOrderId, CancellationToken ct = default)
    {
        var map = await ResolveBatchAsync(new[] { salesOrderId }, ct).ConfigureAwait(false);
        return map.GetValueOrDefault(salesOrderId, SalesOrderWorkflowStatus.Open);
    }

    public async Task<Dictionary<int, string>> ResolveBatchAsync(
        IReadOnlyList<int> salesOrderIds,
        CancellationToken ct = default)
    {
        if (salesOrderIds.Count == 0)
            return new Dictionary<int, string>();

        var ids = salesOrderIds.Distinct().ToList();
        var ctx = await LoadContextAsync(ids, ct).ConfigureAwait(false);
        var result = new Dictionary<int, string>(ids.Count);
        var needsStockCheck = new List<int>();

        foreach (var id in ids)
        {
            var (status, checkStock) = ResolveWithoutStock(id, ctx);
            result[id] = status;
            if (checkStock)
                needsStockCheck.Add(id);
        }

        foreach (var id in needsStockCheck)
        {
            if (!ctx.OrdersById.TryGetValue(id, out var order))
                continue;
            if (!ctx.GiByOrderId.TryGetValue(id, out var giDoc))
                continue;
            var plantId = (order.PlantId ?? "").Trim();
            if (plantId.Length == 0)
            {
                result[id] = SalesOrderWorkflowStatus.PendingStock;
                continue;
            }

            var deductLines = BuildDeductLines(id, giDoc, ctx);
            if (deductLines.Count == 0)
            {
                result[id] = SalesOrderWorkflowStatus.PendingGoodReceive;
                continue;
            }

            var gradeBySoItem = BuildGradeMap(id, ctx);
            var (sufficient, _) = await DeliveryChallanStockService.CheckAvailabilityAsync(
                _db, deductLines, gradeBySoItem, plantId, ct).ConfigureAwait(false);
            result[id] = sufficient
                ? SalesOrderWorkflowStatus.PendingGoodReceive
                : SalesOrderWorkflowStatus.PendingStock;
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<string, int>> CountByStatusAsync(
        SalesOrderWorkflowCountFilter? filter,
        CancellationToken ct = default)
    {
        var orders = await LoadFilteredOrdersAsync(filter, ct).ConfigureAwait(false);
        var ids = orders.Select(o => o.Id).ToList();
        var statuses = await ResolveBatchAsync(ids, ct).ConfigureAwait(false);

        var counts = SalesOrderWorkflowStatus.AllWorkflowStatuses.ToDictionary(s => s, _ => 0);
        foreach (var id in ids)
        {
            if (statuses.TryGetValue(id, out var st))
                counts[st] = counts.GetValueOrDefault(st) + 1;
        }
        return counts;
    }

    private async Task<List<SalesOrder>> LoadFilteredOrdersAsync(
        SalesOrderWorkflowCountFilter? filter,
        CancellationToken ct)
    {
        IQueryable<SalesOrder> query = _db.SalesOrders.AsNoTracking();
        var qq = (filter?.Q ?? "").Trim();
        if (qq.Length > 0)
        {
            query = query.Where(x =>
                x.SalesOrderNumber.Contains(qq)
                || (x.CustomerName != null && x.CustomerName.Contains(qq)));
        }
        if (!string.IsNullOrWhiteSpace(filter?.PlantId))
            query = query.Where(x => x.PlantId == filter.PlantId);
        if (filter?.DistributionChannelId is { } dcid && dcid > 0)
            query = query.Where(x => x.DistributionChannelId == dcid);
        return await query.ToListAsync(ct).ConfigureAwait(false);
    }

    private sealed class WorkflowContext
    {
        public Dictionary<int, SalesOrder> OrdersById { get; init; } = new();
        public Dictionary<int, GiSnapshot> GiByOrderId { get; init; } = new();
        public Dictionary<int, List<GiLineSnapshot>> GiLinesByDocId { get; init; } = new();
        public Dictionary<int, DcSnapshot> DcByOrderId { get; init; } = new();
        public Dictionary<int, InvoiceSnapshot> InvoiceByOrderId { get; init; } = new();
        public Dictionary<int, List<SoLineSnapshot>> SoLinesByOrderId { get; init; } = new();
    }

    private sealed class GiSnapshot
    {
        public int Id { get; init; }
        public string Status { get; init; } = "";
        public string DispatchStatus { get; init; } = "";
    }

    private sealed class GiLineSnapshot
    {
        public string MaterialNumber { get; init; } = "";
        public decimal RemainingQty { get; init; }
        public int RequiredUomId { get; init; }
        public int? SalesOrderItemId { get; init; }
        public string? SalesPriceGrade { get; init; }
    }

    private sealed class SoLineSnapshot
    {
        public int Id { get; init; }
        public string MaterialNumber { get; init; } = "";
        public decimal OrderQuantity { get; init; }
        public int? QuantityUomId { get; init; }
        public string? SalesPriceGrade { get; init; }
    }

    private sealed class DcSnapshot
    {
        public int Id { get; init; }
        public DateTime? DeliveryCompletedAt { get; init; }
    }

    private sealed class InvoiceSnapshot
    {
        public string Status { get; init; } = "";
    }

    private async Task<WorkflowContext> LoadContextAsync(IReadOnlyList<int> salesOrderIds, CancellationToken ct)
    {
        var orders = await _db.SalesOrders.AsNoTracking()
            .Where(o => salesOrderIds.Contains(o.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var giDocs = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .Where(g => salesOrderIds.Contains(g.SalesOrderId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var giByOrder = giDocs
            .GroupBy(g => g.SalesOrderId)
            .ToDictionary(gr => gr.Key, gr => gr.OrderByDescending(x => x.Id).First());

        var giDocIds = giByOrder.Values.Select(g => g.Id).ToList();
        var giLines = giDocIds.Count == 0
            ? new List<SalesGoodsIssueDocumentLine>()
            : await _db.SalesGoodsIssueDocumentLines.AsNoTracking()
                .Where(l => giDocIds.Contains(l.SalesGoodsIssueDocumentId))
                .ToListAsync(ct)
                .ConfigureAwait(false);

        var dcs = await _db.DeliveryChallans.AsNoTracking()
            .Where(d => d.SalesOrderId != null && salesOrderIds.Contains(d.SalesOrderId.Value))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var dcByOrder = dcs
            .GroupBy(d => d.SalesOrderId!.Value)
            .ToDictionary(gr => gr.Key, gr => gr.OrderByDescending(x => x.Id).First());

        var dcIds = dcByOrder.Values.Select(d => d.Id).ToList();
        var invoices = dcIds.Count == 0
            ? new List<SalesInvoice>()
            : await _db.SalesInvoices.AsNoTracking()
                .Where(i => dcIds.Contains(i.DeliveryChallanId))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        var invByDcId = invoices.ToDictionary(i => i.DeliveryChallanId);

        var soItems = await _db.SalesOrderItems.AsNoTracking()
            .Where(i => salesOrderIds.Contains(i.SalesOrderId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new WorkflowContext
        {
            OrdersById = orders.ToDictionary(o => o.Id),
            GiByOrderId = giByOrder.ToDictionary(
                kv => kv.Key,
                kv => new GiSnapshot
                {
                    Id = kv.Value.Id,
                    Status = kv.Value.Status,
                    DispatchStatus = kv.Value.DispatchStatus
                }),
            GiLinesByDocId = giLines
                .GroupBy(l => l.SalesGoodsIssueDocumentId)
                .ToDictionary(
                    gr => gr.Key,
                    gr => gr.Select(l => new GiLineSnapshot
                    {
                        MaterialNumber = l.MaterialNumber,
                        RemainingQty = l.RemainingQty,
                        RequiredUomId = l.RequiredUomId,
                        SalesOrderItemId = l.SalesOrderItemId,
                        SalesPriceGrade = l.SalesPriceGrade
                    }).ToList()),
            DcByOrderId = dcByOrder.ToDictionary(
                kv => kv.Key,
                kv => new DcSnapshot { Id = kv.Value.Id, DeliveryCompletedAt = kv.Value.DeliveryCompletedAt }),
            InvoiceByOrderId = dcByOrder
                .Where(kv => invByDcId.ContainsKey(kv.Value.Id))
                .ToDictionary(
                    kv => kv.Key,
                    kv => new InvoiceSnapshot { Status = invByDcId[kv.Value.Id].Status }),
            SoLinesByOrderId = soItems
                .GroupBy(i => i.SalesOrderId)
                .ToDictionary(
                    gr => gr.Key,
                    gr => gr.Select(i => new SoLineSnapshot
                    {
                        Id = i.Id,
                        MaterialNumber = i.MaterialNumber ?? "",
                        OrderQuantity = i.OrderQuantity,
                        QuantityUomId = i.QuantityUomId,
                        SalesPriceGrade = i.SalesPriceGrade
                    }).ToList())
        };
    }

    private static (string status, bool needsStockCheck) ResolveWithoutStock(int salesOrderId, WorkflowContext ctx)
    {
        if (!ctx.OrdersById.TryGetValue(salesOrderId, out var order))
            return (SalesOrderWorkflowStatus.Open, false);

        if (string.Equals(order.Status, SalesOrder.StatusOpen, StringComparison.OrdinalIgnoreCase))
            return (SalesOrderWorkflowStatus.Open, false);

        if (ctx.InvoiceByOrderId.TryGetValue(salesOrderId, out var inv)
            && string.Equals(inv.Status, SalesInvoice.StatusCollected, StringComparison.OrdinalIgnoreCase))
            return (SalesOrderWorkflowStatus.Completed, false);

        if (ctx.DcByOrderId.TryGetValue(salesOrderId, out var dc))
        {
            if (dc.DeliveryCompletedAt != null
                && ctx.InvoiceByOrderId.TryGetValue(salesOrderId, out var invOpen)
                && string.Equals(invOpen.Status, SalesInvoice.StatusOpen, StringComparison.OrdinalIgnoreCase))
                return (SalesOrderWorkflowStatus.PendingPayment, false);

            if (dc.DeliveryCompletedAt == null)
                return (SalesOrderWorkflowStatus.DeliveryInProcess, false);
        }

        if (ctx.GiByOrderId.TryGetValue(salesOrderId, out var gi)
            && string.Equals(gi.Status, SalesGoodsIssueDocument.StatusReceived, StringComparison.OrdinalIgnoreCase)
            && !ctx.DcByOrderId.ContainsKey(salesOrderId))
            return (SalesOrderWorkflowStatus.PendingDc, false);

        if (!ctx.GiByOrderId.TryGetValue(salesOrderId, out var giDoc))
            return (SalesOrderWorkflowStatus.PendingStock, false);

        if (string.Equals(giDoc.Status, SalesGoodsIssueDocument.StatusReceived, StringComparison.OrdinalIgnoreCase))
            return (SalesOrderWorkflowStatus.PendingDc, false);

        if (string.Equals(giDoc.DispatchStatus, SalesGoodsIssueDocument.DispatchSent, StringComparison.OrdinalIgnoreCase))
            return (SalesOrderWorkflowStatus.PendingGoodReceive, false);

        return (SalesOrderWorkflowStatus.PendingStock, true);
    }

    private static List<DeliveryChallanStockService.LineDeduct> BuildDeductLines(
        int salesOrderId,
        GiSnapshot gi,
        WorkflowContext ctx)
    {
        if (ctx.GiLinesByDocId.TryGetValue(gi.Id, out var giLines) && giLines.Count > 0)
        {
            return giLines
                .Where(l => !string.IsNullOrWhiteSpace(l.MaterialNumber)
                    && l.RequiredUomId > 0
                    && l.RemainingQty > InventoryEpsilon)
                .Select(l => new DeliveryChallanStockService.LineDeduct
                {
                    MaterialNumber = l.MaterialNumber.Trim(),
                    Qty = l.RemainingQty,
                    QuantityUomId = l.RequiredUomId,
                    SalesOrderItemId = l.SalesOrderItemId
                })
                .ToList();
        }

        if (!ctx.SoLinesByOrderId.TryGetValue(salesOrderId, out var soLines))
            return new List<DeliveryChallanStockService.LineDeduct>();

        return soLines
            .Where(i => !string.IsNullOrWhiteSpace(i.MaterialNumber)
                && i.OrderQuantity > 0
                && i.QuantityUomId is > 0)
            .Select(i => new DeliveryChallanStockService.LineDeduct
            {
                MaterialNumber = i.MaterialNumber.Trim(),
                Qty = i.OrderQuantity,
                QuantityUomId = i.QuantityUomId!.Value,
                SalesOrderItemId = i.Id
            })
            .ToList();
    }

    private static Dictionary<int, string?> BuildGradeMap(int salesOrderId, WorkflowContext ctx)
    {
        var map = new Dictionary<int, string?>();
        if (!ctx.SoLinesByOrderId.TryGetValue(salesOrderId, out var lines))
            return map;
        foreach (var ln in lines)
            map[ln.Id] = ln.SalesPriceGrade;
        return map;
    }
}
