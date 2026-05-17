using AU_ERP.Models;
using AU_ERP.Models.Mobile;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services.Mobile;

public class MobileReportingService
{
    private readonly AppDbContext _db;

    public MobileReportingService(AppDbContext db) => _db = db;

    // ─── Dashboard ────────────────────────────────────────────────────────────

    public async Task<MobileDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var today = now.Date;

        // Production batches this month (GrDate is stored as DateTime with date column type)
        var batchesThisMonth = await _db.GoodsProduceBatches
            .AsNoTracking()
            .Where(b => b.GrDate >= monthStart && b.GrDate <= today)
            .Select(b => new { b.ProducedQty, b.RejectedScrapQty })
            .ToListAsync(ct);

        var totalProducedThisMonth = batchesThisMonth.Sum(b => b.ProducedQty);
        var totalScrapThisMonth = batchesThisMonth.Sum(b => b.RejectedScrapQty);
        var defectPct = totalProducedThisMonth > 0
            ? Math.Round(totalScrapThisMonth / totalProducedThisMonth * 100, 2)
            : 0m;

        var ordersThisMonth = await _db.ProductionOrders
            .AsNoTracking()
            .CountAsync(o => o.CreatedAt >= monthStart, ct);

        var activeWorkOrders = await _db.ProductionOrders
            .AsNoTracking()
            .CountAsync(o => o.Status == ProductionOrder.StatusReleased
                          || o.Status == ProductionOrder.StatusInProgress, ct);

        // Sales invoices this month (DocumentDate is DateTime with date column type)
        var invoicesThisMonth = await _db.SalesInvoices
            .AsNoTracking()
            .Where(i => i.DocumentDate >= monthStart && i.DocumentDate <= today)
            .Select(i => new { i.GrandTotal, i.Status })
            .ToListAsync(ct);

        var totalRevenueThisMonth = invoicesThisMonth.Sum(i => i.GrandTotal);
        var pendingInvoicesCount = invoicesThisMonth.Count(i => i.Status == SalesInvoice.StatusOpen);

        // SalesPayment uses DocumentDate (not PaymentDate)
        var collectedThisMonth = await _db.SalesPayments
            .AsNoTracking()
            .Where(p => p.DocumentDate >= monthStart && p.DocumentDate <= today)
            .SumAsync(p => p.Amount, ct);

        var stockLines = await _db.StockInventoryLines
            .AsNoTracking()
            .Where(s => s.Status == StockInventoryLine.StatusActive)
            .Select(s => new { s.StockValue })
            .ToListAsync(ct);

        var totalStockValue = stockLines.Sum(s => s.StockValue);
        var finishedGoodsCount = await _db.StockInventoryLines
            .AsNoTracking()
            .CountAsync(s => s.Status == StockInventoryLine.StatusActive && s.Quantity > 0, ct);

        var returnsThisMonth = await _db.SalesReturnOrders
            .AsNoTracking()
            .CountAsync(r => r.DocumentDate >= monthStart && r.DocumentDate <= today, ct);

        return new MobileDashboardDto(
            ProductionOrdersThisMonth: ordersThisMonth,
            ActiveWorkOrders: activeWorkOrders,
            TotalProductionQtyThisMonth: totalProducedThisMonth,
            DefectPercentThisMonth: defectPct,
            TotalRevenueThisMonth: totalRevenueThisMonth,
            CollectedRevenueThisMonth: collectedThisMonth,
            OutstandingRevenueThisMonth: totalRevenueThisMonth - collectedThisMonth,
            InvoicesThisMonth: invoicesThisMonth.Count,
            PendingInvoices: pendingInvoicesCount,
            TotalStockValue: totalStockValue,
            FinishedGoodsLines: finishedGoodsCount,
            SalesReturnsThisMonth: returnsThisMonth
        );
    }

    // ─── Daily Production ─────────────────────────────────────────────────────

    public async Task<PagedResult<DailyProductionRow>> GetDailyProductionAsync(
        MobileReportFilter filter, CancellationToken ct = default)
    {
        var from = filter.EffectiveDateFrom;
        var to = filter.EffectiveDateTo;

        var query = _db.GoodsProduceBatches
            .AsNoTracking()
            .Where(b => b.GrDate >= from && b.GrDate <= to);

        if (!string.IsNullOrWhiteSpace(filter.PlantId))
        {
            var orderIdsInPlant = _db.ProductionOrderLines
                .Where(l => l.PlantId == filter.PlantId)
                .Select(l => l.ProductionOrderId)
                .Distinct();
            query = query.Where(b => orderIdsInPlant.Contains(b.ProductionOrderId));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(b => b.GrDate)
            .ThenBy(b => b.ProductionOrderId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => new
            {
                b.GrDate,
                b.BatchNo,
                b.ProducedQty,
                b.QtyFirstQuality,
                b.QtySecondQuality,
                b.QtyThirdQuality,
                b.RejectedScrapQty,
                b.ProductionOrderId,
                OrderNo = b.ProductionOrder!.ProductionNumber,
                DocNo = b.ProductionOrder.ProductionDocumentNumber,
                OrderStatus = b.ProductionOrder.Status,
                MaterialDesc = b.Material != null ? b.Material.Description : b.MaterialNumber,
                b.MaterialNumber,
            })
            .ToListAsync(ct);

        var poIds = rows.Select(r => r.ProductionOrderId).Distinct().ToList();
        var wastageByOrder = await _db.ProductionOrderStageProgresses
            .AsNoTracking()
            .Where(s => poIds.Contains(s.ProductionOrderId))
            .GroupBy(s => s.ProductionOrderId)
            .Select(g => new { OrderId = g.Key, TotalWastage = g.Sum(s => s.WastageQuantity ?? 0) })
            .ToDictionaryAsync(x => x.OrderId, x => x.TotalWastage, ct);

        var result = rows.Select(b =>
        {
            var goodQty = b.QtyFirstQuality + b.QtySecondQuality + b.QtyThirdQuality;
            wastageByOrder.TryGetValue(b.ProductionOrderId, out var wastage);
            var defectPct = b.ProducedQty > 0
                ? Math.Round(b.RejectedScrapQty / b.ProducedQty * 100, 2) : 0m;

            return new DailyProductionRow(
                ProductionDate: b.GrDate,
                ProductionOrderNumber: b.OrderNo.ToString(),
                DocumentNumber: b.DocNo,
                MaterialNumber: b.MaterialNumber,
                MaterialDescription: b.MaterialDesc ?? b.MaterialNumber,
                ProducedQty: b.ProducedQty,
                GoodQty: goodQty,
                DefectiveQty: b.RejectedScrapQty,
                WastageQty: wastage,
                DefectPercent: defectPct,
                BatchNo: b.BatchNo,
                Status: b.OrderStatus
            );
        }).ToList();

        return new PagedResult<DailyProductionRow>(result, filter.Page, filter.PageSize, total);
    }

    // ─── Production Summary ───────────────────────────────────────────────────

    public async Task<ProductionSummaryDto> GetProductionSummaryAsync(
        MobileReportFilter filter, CancellationToken ct = default)
    {
        var from = filter.EffectiveDateFrom;
        var to = filter.EffectiveDateTo;

        var batches = await _db.GoodsProduceBatches
            .AsNoTracking()
            .Where(b => b.GrDate >= from && b.GrDate <= to)
            .Select(b => new
            {
                b.GrDate,
                b.ProducedQty,
                b.QtyFirstQuality,
                b.QtySecondQuality,
                b.QtyThirdQuality,
                b.RejectedScrapQty
            })
            .ToListAsync(ct);

        var orderQuery = _db.ProductionOrders.AsNoTracking()
            .Where(o => o.CreatedAt.Date >= from && o.CreatedAt.Date <= to);

        if (!string.IsNullOrWhiteSpace(filter.PlantId))
        {
            var orderIdsInPlant = _db.ProductionOrderLines
                .Where(l => l.PlantId == filter.PlantId)
                .Select(l => l.ProductionOrderId);
            orderQuery = orderQuery.Where(o => orderIdsInPlant.Contains(o.Id));
        }

        var orderStatuses = await orderQuery
            .Select(o => o.Status)
            .ToListAsync(ct);

        var wastage = await _db.ProductionOrderStageProgresses
            .AsNoTracking()
            .Where(s => s.ProductionOrder!.CreatedAt.Date >= from
                     && s.ProductionOrder.CreatedAt.Date <= to)
            .SumAsync(s => s.WastageQuantity ?? 0, ct);

        var totalProduced = batches.Sum(b => b.ProducedQty);
        var totalGood = batches.Sum(b => b.QtyFirstQuality + b.QtySecondQuality + b.QtyThirdQuality);
        var totalDefective = batches.Sum(b => b.RejectedScrapQty);

        var kpis = new ProductionSummaryKpis(
            TotalOrders: orderStatuses.Count,
            TotalProducedQty: totalProduced,
            TotalGoodQty: totalGood,
            TotalDefectiveQty: totalDefective,
            TotalWastageQty: wastage,
            OverallDefectPercent: totalProduced > 0
                ? Math.Round(totalDefective / totalProduced * 100, 2) : 0m,
            CompletedOrders: orderStatuses.Count(s => s == ProductionOrder.StatusCompleted),
            InProgressOrders: orderStatuses.Count(s => s == ProductionOrder.StatusInProgress),
            PlannedOrders: orderStatuses.Count(s => s == ProductionOrder.StatusPlanned),
            ReleasedOrders: orderStatuses.Count(s => s == ProductionOrder.StatusReleased)
        );

        // Weekly chart series grouped by ISO week
        var chartSeries = batches
            .GroupBy(b => $"{b.GrDate.Year}-W{System.Globalization.ISOWeek.GetWeekOfYear(b.GrDate):D2}")
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var produced = g.Sum(b => b.ProducedQty);
                var good = g.Sum(b => b.QtyFirstQuality + b.QtySecondQuality + b.QtyThirdQuality);
                var defective = g.Sum(b => b.RejectedScrapQty);
                return new ProductionSummaryRow(
                    Label: g.Key,
                    OrderCount: g.Count(),
                    ProducedQty: produced,
                    GoodQty: good,
                    DefectiveQty: defective,
                    DefectPercent: produced > 0 ? Math.Round(defective / produced * 100, 2) : 0m
                );
            })
            .ToList();

        return new ProductionSummaryDto(kpis, chartSeries);
    }

    // ─── Defect / Wastage ─────────────────────────────────────────────────────

    public async Task<DefectReportDto> GetDefectsAsync(
        MobileReportFilter filter, CancellationToken ct = default)
    {
        var from = filter.EffectiveDateFrom;
        var to = filter.EffectiveDateTo;

        var query = _db.GoodsProduceBatches
            .AsNoTracking()
            .Where(b => b.GrDate >= from && b.GrDate <= to);

        if (!string.IsNullOrWhiteSpace(filter.PlantId))
        {
            var orderIdsInPlant = _db.ProductionOrderLines
                .Where(l => l.PlantId == filter.PlantId)
                .Select(l => l.ProductionOrderId);
            query = query.Where(b => orderIdsInPlant.Contains(b.ProductionOrderId));
        }

        var total = await query.CountAsync(ct);

        var allBatches = await query
            .Select(b => new { b.ProducedQty, b.QtyFirstQuality, b.QtySecondQuality, b.QtyThirdQuality, b.RejectedScrapQty })
            .ToListAsync(ct);

        var totalStageWastage = await _db.ProductionOrderStageProgresses
            .AsNoTracking()
            .Where(s => s.ProductionOrder!.GoodsProduceBatches
                .Any(b => b.GrDate >= from && b.GrDate <= to))
            .SumAsync(s => s.WastageQuantity ?? 0, ct);

        var kpis = new DefectKpis(
            TotalProduced: allBatches.Sum(b => b.ProducedQty),
            TotalFirstQuality: allBatches.Sum(b => b.QtyFirstQuality),
            TotalSecondQuality: allBatches.Sum(b => b.QtySecondQuality),
            TotalThirdQuality: allBatches.Sum(b => b.QtyThirdQuality),
            TotalScrap: allBatches.Sum(b => b.RejectedScrapQty),
            TotalStageWastage: totalStageWastage,
            ScrapPercent: allBatches.Sum(b => b.ProducedQty) > 0
                ? Math.Round(allBatches.Sum(b => b.RejectedScrapQty) / allBatches.Sum(b => b.ProducedQty) * 100, 2)
                : 0m
        );

        var batches = await query
            .OrderByDescending(b => b.GrDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => new
            {
                b.GrDate,
                b.BatchNo,
                b.ProducedQty,
                b.QtyFirstQuality,
                b.QtySecondQuality,
                b.QtyThirdQuality,
                b.RejectedScrapQty,
                b.MaterialNumber,
                MaterialDesc = b.Material != null ? b.Material.Description : b.MaterialNumber,
                OrderNo = b.ProductionOrder!.ProductionNumber,
                DocNo = b.ProductionOrder.ProductionDocumentNumber,
                PoId = b.ProductionOrderId
            })
            .ToListAsync(ct);

        var poIds = batches.Select(b => b.PoId).Distinct().ToList();
        var stageWastage = await _db.ProductionOrderStageProgresses
            .AsNoTracking()
            .Where(s => poIds.Contains(s.ProductionOrderId))
            .GroupBy(s => s.ProductionOrderId)
            .Select(g => new { g.Key, Total = g.Sum(s => s.WastageQuantity ?? 0) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, ct);

        var rows = batches.Select(b =>
        {
            stageWastage.TryGetValue(b.PoId, out var w);
            var defPct = b.ProducedQty > 0 ? Math.Round(b.RejectedScrapQty / b.ProducedQty * 100, 2) : 0m;
            return new DefectReportRow(
                ProductionOrderNumber: b.OrderNo.ToString(),
                DocumentNumber: b.DocNo,
                MaterialNumber: b.MaterialNumber,
                MaterialDescription: b.MaterialDesc ?? b.MaterialNumber,
                BatchDate: b.GrDate,
                ProducedQty: b.ProducedQty,
                FirstQualityQty: b.QtyFirstQuality,
                SecondQualityQty: b.QtySecondQuality,
                ThirdQualityQty: b.QtyThirdQuality,
                ScrapQty: b.RejectedScrapQty,
                StageWastageQty: w,
                DefectPercent: defPct,
                BatchNo: b.BatchNo
            );
        }).ToList();

        return new DefectReportDto(kpis, new PagedResult<DefectReportRow>(rows, filter.Page, filter.PageSize, total));
    }

    // ─── Raw Material Consumption ─────────────────────────────────────────────

    public async Task<RawMaterialConsumptionDto> GetRawMaterialConsumptionAsync(
        MobileReportFilter filter, CancellationToken ct = default)
    {
        var from = filter.EffectiveDateFrom;
        var to = filter.EffectiveDateTo;

        var query = _db.GoodsIssueDocumentLines
            .AsNoTracking()
            .Where(l => l.GoodsIssueDocument!.DocumentDate >= from
                     && l.GoodsIssueDocument.DocumentDate <= to
                     && l.GoodsIssueDocument.Status == GoodsIssueDocument.StatusCompleted);

        var totalLines = await query.CountAsync(ct);
        var totalIssued = await query.SumAsync(l => l.IssuedQty, ct);

        var rows = await query
            .OrderByDescending(l => l.GoodsIssueDocument!.DocumentDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => new RawMaterialConsumptionRow(
                l.GoodsIssueDocument!.DocumentNumber,
                l.GoodsIssueDocument.DocumentDate,
                l.MaterialNumber,
                l.MaterialDescription ?? l.MaterialNumber,
                l.IssuedQty,
                l.RequiredUom != null ? l.RequiredUom.Code : "",
                l.FertMaterialNumber,
                l.FertMaterialDescription
            ))
            .ToListAsync(ct);

        var kpis = new RawMaterialKpis(
            TotalMaterialsConsumed: totalLines,
            TotalIssuedLines: totalIssued
        );

        return new RawMaterialConsumptionDto(kpis, new PagedResult<RawMaterialConsumptionRow>(rows, filter.Page, filter.PageSize, totalLines));
    }

    // ─── Batch / Lot Tracking ─────────────────────────────────────────────────

    public async Task<PagedResult<BatchTrackingRow>> GetBatchTrackingAsync(
        MobileReportFilter filter, CancellationToken ct = default)
    {
        var from = filter.EffectiveDateFrom;
        var to = filter.EffectiveDateTo;

        var query = _db.GoodsProduceBatches
            .AsNoTracking()
            .Where(b => b.GrDate >= from && b.GrDate <= to);

        if (!string.IsNullOrWhiteSpace(filter.PlantId))
        {
            var orderIdsInPlant = _db.ProductionOrderLines
                .Where(l => l.PlantId == filter.PlantId)
                .Select(l => l.ProductionOrderId);
            query = query.Where(b => orderIdsInPlant.Contains(b.ProductionOrderId));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(b => b.GrDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => new
            {
                b.BatchNo,
                b.GrDate,
                b.MaterialNumber,
                MaterialDesc = b.Material != null ? b.Material.Description : b.MaterialNumber,
                OrderNo = b.ProductionOrder!.ProductionNumber,
                DocNo = b.ProductionOrder.ProductionDocumentNumber,
                b.ProducedQty,
                b.QtyFirstQuality,
                b.QtySecondQuality,
                b.QtyThirdQuality,
                b.RejectedScrapQty
            })
            .ToListAsync(ct);

        static string QualityLabel(decimal first, decimal produced)
        {
            if (produced == 0) return "Unknown";
            var pct = first / produced * 100;
            return pct >= 90 ? "Excellent" : pct >= 70 ? "Good" : pct >= 50 ? "Mixed" : "Poor";
        }

        var result = rows.Select(b => new BatchTrackingRow(
            BatchNo: b.BatchNo,
            ProductionOrderNumber: b.OrderNo.ToString(),
            DocumentNumber: b.DocNo,
            MaterialNumber: b.MaterialNumber,
            MaterialDescription: b.MaterialDesc ?? b.MaterialNumber,
            BatchDate: b.GrDate,
            ProducedQty: b.ProducedQty,
            FirstQualityQty: b.QtyFirstQuality,
            SecondQualityQty: b.QtySecondQuality,
            ThirdQualityQty: b.QtyThirdQuality,
            ScrapQty: b.RejectedScrapQty,
            QualityStatus: QualityLabel(b.QtyFirstQuality, b.ProducedQty)
        )).ToList();

        return new PagedResult<BatchTrackingRow>(result, filter.Page, filter.PageSize, total);
    }

    // ─── Work Orders ──────────────────────────────────────────────────────────

    public async Task<WorkOrdersDto> GetWorkOrdersAsync(
        MobileReportFilter filter, CancellationToken ct = default)
    {
        var from = filter.EffectiveDateFrom;
        var to = filter.EffectiveDateTo;

        var orderQuery = _db.ProductionOrders.AsNoTracking()
            .Where(o => o.CreatedAt.Date >= from && o.CreatedAt.Date <= to);

        if (!string.IsNullOrWhiteSpace(filter.PlantId))
        {
            var orderIdsInPlant = _db.ProductionOrderLines
                .Where(l => l.PlantId == filter.PlantId)
                .Select(l => l.ProductionOrderId);
            orderQuery = orderQuery.Where(o => orderIdsInPlant.Contains(o.Id));
        }

        var statuses = await orderQuery.Select(o => o.Status).ToListAsync(ct);

        var kpis = new WorkOrderKpis(
            Total: statuses.Count,
            Planned: statuses.Count(s => s == ProductionOrder.StatusPlanned),
            Released: statuses.Count(s => s == ProductionOrder.StatusReleased),
            InProgress: statuses.Count(s => s == ProductionOrder.StatusInProgress),
            Completed: statuses.Count(s => s == ProductionOrder.StatusCompleted)
        );

        var orders = await orderQuery
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(o => new
            {
                o.Id,
                o.ProductionNumber,
                o.ProductionDocumentNumber,
                o.FinishedMaterialNumber,
                MaterialDesc = o.FinishedMaterial != null ? o.FinishedMaterial.Description : o.FinishedMaterialNumber,
                o.TargetQuantity,
                UomCode = o.Uom != null ? o.Uom.Code : "",
                o.PlannedStartDate,
                o.PlannedEndDate,
                o.Status,
                o.Priority,
                StagesDone = o.StageProgresses.Count(s => s.StageStatus == ProductionOrderStageProgress.StageCompleted),
                StagesTotal = o.StageProgresses.Count()
            })
            .ToListAsync(ct);

        var rows = orders.Select(o => new WorkOrderRow(
            Id: o.Id,
            ProductionOrderNumber: o.ProductionNumber.ToString(),
            DocumentNumber: o.ProductionDocumentNumber,
            MaterialNumber: o.FinishedMaterialNumber,
            MaterialDescription: o.MaterialDesc ?? o.FinishedMaterialNumber,
            TargetQuantity: o.TargetQuantity,
            Uom: o.UomCode,
            PlannedStartDate: o.PlannedStartDate,
            PlannedEndDate: o.PlannedEndDate,
            Status: o.Status,
            Priority: o.Priority,
            StagesDone: o.StagesDone,
            StagesTotal: o.StagesTotal
        )).ToList();

        return new WorkOrdersDto(kpis, new PagedResult<WorkOrderRow>(rows, filter.Page, filter.PageSize, statuses.Count));
    }

    // ─── Finished Goods Inventory ─────────────────────────────────────────────

    public async Task<FinishedGoodsDto> GetFinishedGoodsAsync(
        MobileReportFilter filter, CancellationToken ct = default)
    {
        var query = _db.StockInventoryLines
            .AsNoTracking()
            .Where(s => s.Status == StockInventoryLine.StatusActive
                     && s.Material != null
                     && s.Material.MaterialTypeCode == "FERT");

        if (!string.IsNullOrWhiteSpace(filter.PlantId))
            query = query.Where(s => s.PlantID == filter.PlantId);

        var allLines = await query
            .Select(s => new { s.StockValue, s.Quantity })
            .ToListAsync(ct);

        var kpis = new FinishedGoodsKpis(
            TotalLines: allLines.Count,
            TotalStockValue: allLines.Sum(s => s.StockValue),
            ZeroStockLines: allLines.Count(s => s.Quantity == 0)
        );

        var rows = await query
            .OrderBy(s => s.Material!.Description)
            .ThenBy(s => s.Grade)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(s => new FinishedGoodsRow(
                s.MaterialNumber,
                s.Material!.Description ?? s.MaterialNumber,
                s.PlantID,
                s.Plant != null ? s.Plant.PlantName : s.PlantID,
                s.Quantity,
                s.QuantityUom != null ? s.QuantityUom.Code : "",
                s.Grade,
                s.BatchOrLot,
                s.StandardCostPerUom,
                s.StockValue
            ))
            .ToListAsync(ct);

        return new FinishedGoodsDto(kpis, new PagedResult<FinishedGoodsRow>(rows, filter.Page, filter.PageSize, allLines.Count));
    }

    // ─── Sales Summary ────────────────────────────────────────────────────────

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(
        MobileSalesFilter filter, CancellationToken ct = default)
    {
        var from = filter.EffectiveDateFrom;
        var to = filter.EffectiveDateTo;

        var invoiceQuery = _db.SalesInvoices
            .AsNoTracking()
            .Where(i => i.DocumentDate >= from && i.DocumentDate <= to);

        if (!string.IsNullOrWhiteSpace(filter.CustomerId))
            invoiceQuery = invoiceQuery.Where(i => i.DealerBusinessPartnerId == filter.CustomerId);

        var invoices = await invoiceQuery
            .Select(i => new { i.GrandTotal, i.Status })
            .ToListAsync(ct);

        var totalRevenue = invoices.Sum(i => i.GrandTotal);

        // SalesPayment.DocumentDate is the payment date field
        var collectedRevenue = await _db.SalesPayments
            .AsNoTracking()
            .Where(p => p.DocumentDate >= from && p.DocumentDate <= to)
            .SumAsync(p => p.Amount, ct);

        // SalesOrder uses OrderDate (not CreatedAt)
        var totalOrders = await _db.SalesOrders
            .AsNoTracking()
            .CountAsync(o => o.OrderDate >= from && o.OrderDate <= to, ct);

        var returnsCreditTotal = await _db.SalesReturnCreditMemos
            .AsNoTracking()
            .Where(c => c.DocumentDate >= from && c.DocumentDate <= to)
            .SumAsync(c => c.GrandTotalCredit, ct);

        var returnsCount = await _db.SalesReturnOrders
            .AsNoTracking()
            .CountAsync(r => r.DocumentDate >= from && r.DocumentDate <= to, ct);

        var kpis = new SalesSummaryKpis(
            TotalRevenue: totalRevenue,
            CollectedRevenue: collectedRevenue,
            OutstandingRevenue: totalRevenue - collectedRevenue,
            TotalInvoices: invoices.Count,
            PaidInvoices: invoices.Count(i => i.Status == SalesInvoice.StatusCollected),
            UnpaidInvoices: invoices.Count(i => i.Status == SalesInvoice.StatusOpen),
            ReturnInProcessInvoices: invoices.Count(i =>
                i.Status == SalesInvoice.StatusReturnInProcess || i.Status == SalesInvoice.StatusReturned),
            AverageInvoiceValue: invoices.Count > 0 ? Math.Round(totalRevenue / invoices.Count, 2) : 0m,
            TotalOrders: totalOrders,
            TotalReturns: returnsCount,
            TotalReturnCreditValue: returnsCreditTotal
        );

        // Monthly chart series
        var monthlyRevenue = await invoiceQuery
            .GroupBy(i => new { i.DocumentDate.Year, i.DocumentDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(i => i.GrandTotal) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync(ct);

        var collectedByMonth = await _db.SalesPayments
            .AsNoTracking()
            .Where(p => p.DocumentDate >= from && p.DocumentDate <= to)
            .GroupBy(p => new { p.DocumentDate.Year, p.DocumentDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Collected = g.Sum(p => p.Amount) })
            .ToListAsync(ct);

        var collectedLookup = collectedByMonth.ToDictionary(
            x => $"{x.Year}-{x.Month:D2}", x => x.Collected);

        var chartSeries = monthlyRevenue.Select(m =>
        {
            var key = $"{m.Year}-{m.Month:D2}";
            collectedLookup.TryGetValue(key, out var collected);
            return new SalesSummaryChartPoint($"{m.Year}-{m.Month:D2}", m.Revenue, collected);
        }).ToList();

        return new SalesSummaryDto(kpis, chartSeries);
    }

    // ─── Invoices ─────────────────────────────────────────────────────────────

    public async Task<PagedResult<InvoiceRow>> GetInvoicesAsync(
        MobileSalesFilter filter, CancellationToken ct = default)
    {
        var from = filter.EffectiveDateFrom;
        var to = filter.EffectiveDateTo;

        var query = _db.SalesInvoices
            .AsNoTracking()
            .Where(i => i.DocumentDate >= from && i.DocumentDate <= to);

        if (!string.IsNullOrWhiteSpace(filter.CustomerId))
            query = query.Where(i => i.DealerBusinessPartnerId == filter.CustomerId);

        if (!string.IsNullOrWhiteSpace(filter.MaterialNumber))
            query = query.Where(i => i.Lines.Any(l => l.MaterialNumber == filter.MaterialNumber));

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(i => i.DocumentDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(i => new InvoiceRow(
                i.Id,
                i.DocumentNumber,
                i.DocumentDate,
                i.DueDate,
                i.DealerDisplayName,
                i.DcNumber,
                i.Subtotal,
                i.GrandTotal,
                i.Status
            ))
            .ToListAsync(ct);

        return new PagedResult<InvoiceRow>(rows, filter.Page, filter.PageSize, total);
    }

    // ─── Customer Wise Sales ──────────────────────────────────────────────────

    public async Task<PagedResult<CustomerSalesRow>> GetCustomerSalesAsync(
        MobileSalesFilter filter, CancellationToken ct = default)
    {
        var from = filter.EffectiveDateFrom;
        var to = filter.EffectiveDateTo;

        var invoices = await _db.SalesInvoices
            .AsNoTracking()
            .Where(i => i.DocumentDate >= from && i.DocumentDate <= to)
            .Select(i => new
            {
                i.DealerBusinessPartnerId,
                i.DealerDisplayName,
                i.GrandTotal,
                i.DocumentDate,
                IsPaid = i.Status == SalesInvoice.StatusCollected,
                PaymentAmount = i.SalesPayment != null ? (decimal?)i.SalesPayment.Amount : null
            })
            .ToListAsync(ct);

        var grouped = invoices
            .GroupBy(i => i.DealerBusinessPartnerId ?? "__walk_in__")
            .Select(g =>
            {
                var total = g.Sum(i => i.GrandTotal);
                var collected = g.Sum(i => i.PaymentAmount ?? 0);
                return new CustomerSalesRow(
                    CustomerId: g.Key == "__walk_in__" ? null : g.Key,
                    CustomerName: g.FirstOrDefault(i => i.DealerDisplayName != null)?.DealerDisplayName ?? "Walk-in",
                    InvoiceCount: g.Count(),
                    TotalAmount: total,
                    CollectedAmount: collected,
                    OutstandingAmount: total - collected,
                    LastInvoiceDate: g.Max(i => (DateTime?)i.DocumentDate)
                );
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        var total2 = grouped.Count;
        var paged = grouped
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return new PagedResult<CustomerSalesRow>(paged, filter.Page, filter.PageSize, total2);
    }

    // ─── Product Wise Sales ───────────────────────────────────────────────────

    public async Task<PagedResult<ProductSalesRow>> GetProductSalesAsync(
        MobileSalesFilter filter, CancellationToken ct = default)
    {
        var from = filter.EffectiveDateFrom;
        var to = filter.EffectiveDateTo;

        var linesQuery = _db.SalesInvoiceLines
            .AsNoTracking()
            .Where(l => l.SalesInvoice!.DocumentDate >= from
                     && l.SalesInvoice.DocumentDate <= to);

        if (!string.IsNullOrWhiteSpace(filter.MaterialNumber))
            linesQuery = linesQuery.Where(l => l.MaterialNumber == filter.MaterialNumber);

        if (!string.IsNullOrWhiteSpace(filter.CustomerId))
            linesQuery = linesQuery.Where(l => l.SalesInvoice!.DealerBusinessPartnerId == filter.CustomerId);

        var lines = await linesQuery
            .Select(l => new
            {
                l.MaterialNumber,
                l.MaterialDescription,
                l.Quantity,
                l.LineTotal,
                UomCode = l.QuantityUom != null ? l.QuantityUom.Code : null
            })
            .ToListAsync(ct);

        var grouped = lines
            .GroupBy(l => l.MaterialNumber)
            .Select(g => new ProductSalesRow(
                MaterialNumber: g.Key,
                MaterialDescription: g.FirstOrDefault(l => l.MaterialDescription != null)?.MaterialDescription ?? g.Key,
                TotalQty: g.Sum(l => l.Quantity),
                Uom: g.FirstOrDefault(l => l.UomCode != null)?.UomCode,
                TotalRevenue: g.Sum(l => l.LineTotal),
                InvoiceLineCount: g.Count()
            ))
            .OrderByDescending(r => r.TotalRevenue)
            .ToList();

        var total = grouped.Count;
        var paged = grouped
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return new PagedResult<ProductSalesRow>(paged, filter.Page, filter.PageSize, total);
    }

    // ─── Sales Returns ────────────────────────────────────────────────────────

    public async Task<SalesReturnsDto> GetSalesReturnsAsync(
        MobileSalesFilter filter, CancellationToken ct = default)
    {
        var from = filter.EffectiveDateFrom;
        var to = filter.EffectiveDateTo;

        var query = _db.SalesReturnOrders
            .AsNoTracking()
            .Where(r => r.DocumentDate >= from && r.DocumentDate <= to);

        if (!string.IsNullOrWhiteSpace(filter.CustomerId))
            query = query.Where(r => r.DealerBusinessPartnerId == filter.CustomerId);

        var allReturns = await query
            .Select(r => new
            {
                r.Id,
                r.DocumentNumber,
                r.DocumentDate,
                r.DealerDisplayName,
                r.InvoiceDocumentNumber,
                r.InvoiceGrandTotal,
                r.ReturnReason,
                HasQI = r.SalesReturnQualityInspection != null,
                HasCM = r.SalesReturnCreditMemo != null,
                CreditAmount = r.SalesReturnCreditMemo != null
                    ? (decimal?)r.SalesReturnCreditMemo.GrandTotalCredit
                    : null
            })
            .ToListAsync(ct);

        var kpis = new SalesReturnKpis(
            TotalReturns: allReturns.Count,
            TotalReturnValue: allReturns.Sum(r => r.InvoiceGrandTotal),
            PendingQualityInspection: allReturns.Count(r => !r.HasQI),
            CreditMemoIssued: allReturns.Count(r => r.HasCM),
            TotalCreditIssued: allReturns.Where(r => r.CreditAmount.HasValue).Sum(r => r.CreditAmount!.Value)
        );

        var paged = allReturns
            .OrderByDescending(r => r.DocumentDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new SalesReturnRow(
                Id: r.Id,
                DocumentNumber: r.DocumentNumber,
                DocumentDate: r.DocumentDate,
                CustomerName: r.DealerDisplayName,
                InvoiceNumber: r.InvoiceDocumentNumber,
                InvoiceTotal: r.InvoiceGrandTotal,
                ReturnReason: r.ReturnReason,
                HasQualityInspection: r.HasQI,
                HasCreditMemo: r.HasCM,
                CreditAmount: r.CreditAmount
            ))
            .ToList();

        return new SalesReturnsDto(kpis, new PagedResult<SalesReturnRow>(paged, filter.Page, filter.PageSize, allReturns.Count));
    }

    // ─── Lookups ──────────────────────────────────────────────────────────────

    public async Task<List<MobileLookupItem>> GetPlantsLookupAsync(string? search, int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take <= 0 ? 50 : take, 1, 100);
        var query = _db.PlantsSamples.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.PlantID.Contains(term) ||
                (p.PlantName != null && p.PlantName.Contains(term)));
        }

        return await query
            .OrderBy(p => p.PlantName)
            .Take(take)
            .Select(p => new MobileLookupItem(p.PlantID, p.PlantName ?? p.PlantID))
            .ToListAsync(ct);
    }

    public async Task<List<MobileLookupItem>> GetCustomersLookupAsync(string? search, int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take <= 0 ? 50 : take, 1, 100);
        var query = _db.BusinessPartnerMasterSamples.AsNoTracking().Where(bp => bp.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(bp =>
                bp.BPID.Contains(term) ||
                (bp.FullName != null && bp.FullName.Contains(term)));
        }

        return await query
            .OrderBy(bp => bp.FullName)
            .Take(take)
            .Select(bp => new MobileLookupItem(bp.BPID, bp.FullName ?? bp.BPID))
            .ToListAsync(ct);
    }

    public async Task<List<MobileProductLookupItem>> GetProductsLookupAsync(string? search, int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take <= 0 ? 50 : take, 1, 100);
        var query = _db.CreateMaterialMaster.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                m.MaterialNumber.Contains(term) ||
                (m.Description != null && m.Description.Contains(term)));
        }

        return await query
            .OrderBy(m => m.MaterialNumber)
            .Take(take)
            .Select(m => new MobileProductLookupItem(m.MaterialNumber, m.Description ?? m.MaterialNumber))
            .ToListAsync(ct);
    }
}
