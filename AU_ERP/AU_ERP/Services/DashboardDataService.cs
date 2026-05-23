using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

public sealed class DashboardDataService
{
    private readonly AppDbContext _db;
    private readonly SalesOrderWorkflowStatusResolver _orderWorkflow;

    public DashboardDataService(AppDbContext db, SalesOrderWorkflowStatusResolver orderWorkflow)
    {
        _db = db;
        _orderWorkflow = orderWorkflow;
    }

    public async Task<DashboardPageVm> BuildAsync(ClaimsPrincipal user, string? period = null, CancellationToken ct = default)
    {
        var display = user.Identity?.Name?.Trim();
        if (string.IsNullOrEmpty(display) && user.Identity is ClaimsIdentity id)
        {
            display = id.FindFirst(ClaimTypes.Email)?.Value
                ?? id.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        if (string.IsNullOrEmpty(display)) display = "User";

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        IReadOnlyList<string> deptNames;
        if (!string.IsNullOrEmpty(userId))
        {
            var rows = await _db.ApplicationUserDepartments
                .AsNoTracking()
                .Where(ud => ud.UserId == userId)
                .Select(ud => new { ud.Department.Name })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            deptNames = rows.Select(x => x.Name).OrderBy(n => n).ToList();
        }
        else
        {
            deptNames = Array.Empty<string>();
        }

        var showAd = user.HasClaim(AuClaimTypes.Department, "Admin");
        var showSt = user.HasClaim(AuClaimTypes.Department, "Store");
        var showSa = user.HasClaim(AuClaimTypes.Department, "Sales");
        var showPr = user.HasClaim(AuClaimTypes.Department, "Production");
        var hasAny = showAd || showSt || showSa || showPr;

        var periodKey = NormalizePeriod(period);
        var window = ResolvePeriodWindow(periodKey);
        var today = DateTime.Today;

        AdminModuleStats? admin = null;
        if (showAd)
            admin = await BuildAdminAsync(ct).ConfigureAwait(false);

        StoreModuleStats? store = null;
        if (showSt)
        {
            var storePlant = user.FindFirst(AuClaimTypes.StorePlant)?.Value?.Trim();
            store = await BuildStoreAsync(storePlant, window.From, window.To, ct).ConfigureAwait(false);
        }

        SalesModuleStats? sales = null;
        if (showSa)
            sales = await BuildSalesAsync(window.From, window.To, today, ct).ConfigureAwait(false);

        ProductionModuleStats? prod = null;
        if (showPr)
            prod = await BuildProductionAsync(window.From, window.To, ct).ConfigureAwait(false);

        var headline = await BuildHeadlineAsync(
            showSa, showSt, showPr,
            window, window.PreviousFrom, window.PreviousTo,
            user.FindFirst(AuClaimTypes.StorePlant)?.Value?.Trim(),
            today, ct).ConfigureAwait(false);

        return new DashboardPageVm
        {
            UserDisplayName = display,
            DepartmentNames = deptNames,
            TodayLabel = today.ToString("dddd, dd MMM yyyy", CultureInfo.InvariantCulture),
            Period = periodKey,
            PeriodLabel = window.Label,
            PeriodFrom = window.From,
            PeriodTo = window.To,
            ShowAdmin = showAd,
            ShowStore = showSt,
            ShowSales = showSa,
            ShowProduction = showPr,
            HasAnyModule = hasAny,
            Headline = headline,
            Admin = admin,
            Store = store,
            Sales = sales,
            Production = prod
        };
    }

    private async Task<AdminModuleStats> BuildAdminAsync(CancellationToken ct)
    {
        var mat = await _db.CreateMaterialMaster.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
        var bp = await _db.BusinessPartnerMasterSamples.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
        var bom = await _db.BomHeadersSamples.AsNoTracking().ActiveMaster().CountAsync(ct).ConfigureAwait(false);
        var wc = await _db.WorkCenterMasterSamples.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
        var rt = await _db.RoutingHeadersSamples.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
        var ucnt = await _db.Users.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
        var drivers = await _db.Drivers.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
        var vehicles = await _db.Vehicles.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
        var docRanges = await _db.DocumentRanges.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
        var docIntegrations = await _db.DocumentIntegrations.AsNoTracking().CountAsync(ct).ConfigureAwait(false);

        var byType = await _db.CreateMaterialMaster.AsNoTracking()
            .GroupBy(m => m.MaterialTypeCode ?? "—")
            .Select(g => new { g.Key, C = g.Count() })
            .OrderByDescending(x => x.C)
            .Take(6)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new AdminModuleStats
        {
            MaterialCount = mat,
            BusinessPartnerCount = bp,
            BomCount = bom,
            WorkCentreCount = wc,
            RoutingCount = rt,
            UserCount = ucnt,
            DriverCount = drivers,
            VehicleCount = vehicles,
            DocumentRangeCount = docRanges,
            DocumentIntegrationCount = docIntegrations,
            BarLabels = new[] { "Materials", "Business partners", "BOMs", "Work centres", "Routings", "Users" },
            BarValues = new[] { mat, bp, bom, wc, rt, ucnt },
            MaterialByType = byType.Select(x => new LabelCountDto { Label = x.Key, Count = x.C, Value = x.C }).ToList()
        };
    }

    private async Task<StoreModuleStats> BuildStoreAsync(
        string? storePlant, DateTime from, DateTime to, CancellationToken ct)
    {
        List<StockInventoryLine> lines;
        if (string.IsNullOrEmpty(storePlant))
            lines = new List<StockInventoryLine>();
        else
        {
            lines = await _db.StockInventoryLines.AsNoTracking()
                .Where(s => s.Status == StockInventoryLine.StatusActive && s.PlantID == storePlant)
                .Include(s => s.Material)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        var byGrade = lines
            .GroupBy(s => string.IsNullOrEmpty(s.Grade) ? "—" : s.Grade)
            .Select(g => new LabelCountDto
            {
                Label = g.Key,
                Count = g.Count(),
                Value = g.Sum(x => x.StockValue)
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var qtyByMaterial = lines
            .GroupBy(s => s.MaterialNumber)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var reorderMaterials = await _db.CreateMaterialMaster.AsNoTracking()
            .Where(m => m.ReorderPoint != null && m.ReorderPoint > 0)
            .Select(m => new { m.MaterialNumber, m.ReorderPoint })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var lowStock = reorderMaterials.Count(m =>
            qtyByMaterial.TryGetValue(m.MaterialNumber, out var qty)
                ? qty <= m.ReorderPoint!.Value
                : true);

        var movementsQ = _db.StockMovements.AsNoTracking()
            .Where(m => m.MovementDate >= from && m.MovementDate <= to);
        if (!string.IsNullOrEmpty(storePlant))
            movementsQ = movementsQ.Where(m => m.FromPlantId == storePlant || m.ToPlantId == storePlant);

        var movements = await movementsQ
            .Select(m => m.MovementDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var movementsByDay = BuildDailySeries(from, to, movements.Select(d => d.Date));

        var grCount = await _db.GoodReceiptDocuments.AsNoTracking()
            .CountAsync(d => d.DocumentDate >= from && d.DocumentDate <= to, ct)
            .ConfigureAwait(false);

        var giCount = await _db.GoodsIssueDocuments.AsNoTracking()
            .CountAsync(d =>
                d.DocumentDate >= from && d.DocumentDate <= to
                && d.Status != GoodsIssueDocument.StatusDraft, ct)
            .ConfigureAwait(false);

        var topStock = lines
            .OrderByDescending(s => s.StockValue)
            .Take(5)
            .Select(s => new DashboardTopStockRowVm
            {
                MaterialNumber = s.MaterialNumber,
                MaterialDescription = s.Material != null ? s.Material.Description : null,
                Grade = string.IsNullOrEmpty(s.Grade) ? "—" : s.Grade,
                Quantity = s.Quantity,
                StockValue = s.StockValue
            })
            .ToList();

        return new StoreModuleStats
        {
            ActiveStockLineCount = lines.Count,
            DistinctMaterialCount = lines.Select(s => s.MaterialNumber).Distinct().Count(),
            TotalStockValue = lines.Sum(s => s.StockValue),
            LowStockCount = lowStock,
            StockMovementsInPeriod = movements.Count,
            GoodsReceiptDocsInPeriod = grCount,
            GoodsIssueDocsInPeriod = giCount,
            ValueByGrade = byGrade,
            MovementsByDay = movementsByDay,
            TopStockByValue = topStock
        };
    }

    private async Task<SalesModuleStats> BuildSalesAsync(
        DateTime from, DateTime to, DateTime today, CancellationToken ct)
    {
        var qDraft = await _db.SalesQuotations.AsNoTracking()
            .CountAsync(x => x.Status == SalesQuotation.StatusDraft, ct).ConfigureAwait(false);
        var qSent = await _db.SalesQuotations.AsNoTracking()
            .CountAsync(x => x.Status == SalesQuotation.StatusSent, ct).ConfigureAwait(false);
        var workflowCounts = await _orderWorkflow.CountByStatusAsync(null, ct).ConfigureAwait(false);
        var oOpen = workflowCounts.GetValueOrDefault(SalesOrderWorkflowStatus.Open);
        var oConf = SalesOrderWorkflowStatus.ConfirmedWorkflowStatuses
            .Sum(s => workflowCounts.GetValueOrDefault(s));
        var orderWorkflowChart = SalesOrderWorkflowStatus.AllWorkflowStatuses
            .Select(s => new LabelCountDto
            {
                Label = s,
                Count = workflowCounts.GetValueOrDefault(s),
                Value = workflowCounts.GetValueOrDefault(s)
            })
            .Where(x => x.Count > 0)
            .ToList();
        var dcTotal = await _db.DeliveryChallans.AsNoTracking().CountAsync(ct).ConfigureAwait(false);

        var qInPeriod = await _db.SalesQuotations.AsNoTracking()
            .CountAsync(x => x.QuotationDate >= from && x.QuotationDate <= to, ct).ConfigureAwait(false);
        var oInPeriod = await _db.SalesOrders.AsNoTracking()
            .CountAsync(x => x.OrderDate >= from && x.OrderDate <= to, ct).ConfigureAwait(false);
        decimal? conversion = qInPeriod > 0
            ? Math.Round((decimal)oInPeriod / qInPeriod * 100m, 1)
            : null;

        var trendFrom = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        var quots = await _db.SalesQuotations.AsNoTracking()
            .Where(x => x.QuotationDate >= trendFrom)
            .Select(x => x.QuotationDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var orders = await _db.SalesOrders.AsNoTracking()
            .Where(x => x.OrderDate >= trendFrom)
            .Select(x => x.OrderDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var qm = new List<LabelCountDto>();
        var om = new List<LabelCountDto>();
        for (var i = 0; i < 6; i++)
        {
            var t = trendFrom.AddMonths(i);
            var key = t.ToString("MMM yyyy", CultureInfo.InvariantCulture);
            qm.Add(new LabelCountDto
            {
                Label = key,
                Count = quots.Count(x => x.Year == t.Year && x.Month == t.Month),
                Value = quots.Count(x => x.Year == t.Year && x.Month == t.Month)
            });
            om.Add(new LabelCountDto
            {
                Label = key,
                Count = orders.Count(x => x.Year == t.Year && x.Month == t.Month),
                Value = orders.Count(x => x.Year == t.Year && x.Month == t.Month)
            });
        }

        var dcCreated = await _db.DeliveryChallans.AsNoTracking()
            .CountAsync(d => d.DocumentDate >= from && d.DocumentDate <= to, ct).ConfigureAwait(false);
        var dcDelivered = await _db.DeliveryChallans.AsNoTracking()
            .CountAsync(d => d.DeliveryCompletedAt != null, ct).ConfigureAwait(false);
        var dcInTransit = await _db.DeliveryChallans.AsNoTracking()
            .CountAsync(d => d.DeliveryCompletedAt == null, ct).ConfigureAwait(false);

        var sgiPending = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .CountAsync(d =>
                d.DocumentDate >= from && d.DocumentDate <= to
                && d.Status == SalesGoodsIssueDocument.StatusPending, ct)
            .ConfigureAwait(false);
        var sgiReceived = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .CountAsync(d =>
                d.DocumentDate >= from && d.DocumentDate <= to
                && d.Status == SalesGoodsIssueDocument.StatusReceived, ct)
            .ConfigureAwait(false);

        var recentDcs = await _db.DeliveryChallans.AsNoTracking()
            .OrderByDescending(d => d.DocumentDate).ThenByDescending(d => d.Id)
            .Take(5)
            .Select(d => new DashboardRecentDcRowVm
            {
                Id = d.Id,
                DocumentNumber = d.DeliveryChallanNumber,
                DealerDisplayName = d.ShipToDisplayName,
                StatusLabel = d.DeliveryCompletedAt != null ? "Delivered" : "In transit",
                DocumentDate = d.DocumentDate
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var invOpenQ = _db.SalesInvoices.AsNoTracking().Where(i => i.Status == SalesInvoice.StatusOpen);
        var invOverdueQ = invOpenQ.Where(i => i.DueDate < today);
        var invOpenCount = await invOpenQ.CountAsync(ct).ConfigureAwait(false);
        var invOverdueCount = await invOverdueQ.CountAsync(ct).ConfigureAwait(false);
        var invCollectedCount = await _db.SalesInvoices.AsNoTracking()
            .CountAsync(i => i.Status == SalesInvoice.StatusCollected, ct).ConfigureAwait(false);
        var invRipCount = await _db.SalesInvoices.AsNoTracking()
            .CountAsync(i => i.Status == SalesInvoice.StatusReturnInProcess, ct).ConfigureAwait(false);
        var invReturnedCount = await _db.SalesInvoices.AsNoTracking()
            .CountAsync(i => i.Status == SalesInvoice.StatusReturned, ct).ConfigureAwait(false);

        var openAmount = await invOpenQ.SumAsync(i => (decimal?)i.GrandTotal, ct).ConfigureAwait(false) ?? 0m;
        var outstanding = openAmount;

        var collectedInPeriod = await _db.SalesPayments.AsNoTracking()
            .Where(p => p.DocumentDate >= from && p.DocumentDate <= to)
            .SumAsync(p => (decimal?)p.Amount, ct).ConfigureAwait(false) ?? 0m;

        var cmInPeriod = await _db.SalesReturnCreditMemos.AsNoTracking()
            .Where(c => c.DocumentDate >= from && c.DocumentDate <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var paymentsInPeriod = await _db.SalesPayments.AsNoTracking()
            .Where(p => p.DocumentDate >= from && p.DocumentDate <= to)
            .CountAsync(ct)
            .ConfigureAwait(false);

        var roOpen = await _db.SalesReturnOrders.AsNoTracking()
            .CountAsync(r => !_db.SalesReturnCreditMemos.Any(c => c.SalesReturnOrderId == r.Id), ct)
            .ConfigureAwait(false);
        var qiPending = await _db.SalesReturnQualityInspections.AsNoTracking()
            .CountAsync(q => q.Status == SalesReturnQualityInspection.StatusPending, ct)
            .ConfigureAwait(false);

        var invoiceDates = await _db.SalesInvoices.AsNoTracking()
            .Where(i => i.DocumentDate >= from && i.DocumentDate <= to)
            .Select(i => i.DocumentDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var paymentDates = await _db.SalesPayments.AsNoTracking()
            .Where(p => p.DocumentDate >= from && p.DocumentDate <= to)
            .Select(p => new { p.DocumentDate, p.Amount })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var invoicedByDay = BuildDailySeries(from, to, invoiceDates.Select(d => d));
        var paymentsByDay = BuildDailyAmountSeries(from, to, paymentDates.Select(p => (p.DocumentDate, p.Amount)));

        var overdueRaw = await _db.SalesInvoices.AsNoTracking()
            .Where(i => i.Status == SalesInvoice.StatusOpen && i.DueDate < today)
            .OrderBy(i => i.DueDate)
            .Take(5)
            .Select(i => new
            {
                i.Id,
                i.DocumentNumber,
                i.DealerDisplayName,
                i.DueDate,
                i.GrandTotal
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var overdueRows = overdueRaw.Select(i => new DashboardOverdueInvoiceRowVm
        {
            Id = i.Id,
            DocumentNumber = i.DocumentNumber,
            DealerDisplayName = i.DealerDisplayName,
            DueDate = i.DueDate,
            DaysOverdue = (today - i.DueDate).Days,
            Balance = i.GrandTotal
        }).ToList();

        return new SalesModuleStats
        {
            QuotationDraft = qDraft,
            QuotationSent = qSent,
            QuotationsCreatedInPeriod = qInPeriod,
            QuotationToOrderConversionPercent = conversion,
            QuotationByMonth = qm,
            SalesOrderByMonth = om,
            OrderOpen = oOpen,
            OrderConfirmed = oConf,
            OrderPendingStock = workflowCounts.GetValueOrDefault(SalesOrderWorkflowStatus.PendingStock),
            OrderPendingGoodReceive = workflowCounts.GetValueOrDefault(SalesOrderWorkflowStatus.PendingGoodReceive),
            OrderPendingDc = workflowCounts.GetValueOrDefault(SalesOrderWorkflowStatus.PendingDc),
            OrderDeliveryInProcess = workflowCounts.GetValueOrDefault(SalesOrderWorkflowStatus.DeliveryInProcess),
            OrderPendingPayment = workflowCounts.GetValueOrDefault(SalesOrderWorkflowStatus.PendingPayment),
            OrderCompleted = workflowCounts.GetValueOrDefault(SalesOrderWorkflowStatus.Completed),
            OrderWorkflowByStatus = orderWorkflowChart,
            DeliveryChallanCount = dcTotal,
            DeliveryChallansCreatedInPeriod = dcCreated,
            DeliveryChallansDelivered = dcDelivered,
            DeliveryChallansInTransit = dcInTransit,
            SalesGoodsIssuePendingInPeriod = sgiPending,
            SalesGoodsIssueReceivedInPeriod = sgiReceived,
            RecentDeliveryChallans = recentDcs,
            InvoiceOpenCount = invOpenCount,
            InvoiceOverdueCount = invOverdueCount,
            InvoiceCollectedCount = invCollectedCount,
            InvoiceReturnInProcessCount = invRipCount,
            InvoiceReturnedCount = invReturnedCount,
            InvoiceOpenAmount = openAmount,
            InvoiceOutstandingAmount = outstanding,
            InvoiceCollectedAmountInPeriod = collectedInPeriod,
            CreditMemosInPeriod = cmInPeriod.Count,
            CreditMemosAmountInPeriod = cmInPeriod.Sum(c => c.GrandTotalCredit),
            PaymentsInPeriod = paymentsInPeriod,
            PaymentsAmountInPeriod = collectedInPeriod,
            ReturnOrdersOpenCount = roOpen,
            ReturnQiPendingCount = qiPending,
            InvoicedVsPaymentsByDay = invoicedByDay,
            PaymentsCollectedByDay = paymentsByDay,
            RecentOverdueInvoices = overdueRows
        };
    }

    private async Task<ProductionModuleStats> BuildProductionAsync(
        DateTime from, DateTime to, CancellationToken ct)
    {
        var bySt = await _db.ProductionOrders.AsNoTracking()
            .GroupBy(p => p.Status)
            .Select(g => new { g.Key, C = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var map = bySt.ToDictionary(x => x.Key, x => x.C);
        var fert = await _db.CreateMaterialMaster.AsNoTracking()
            .CountAsync(m => m.MaterialTypeCode == "FERT" || m.MaterialTypeCode == "HALB", ct)
            .ConfigureAwait(false);
        var ot = await _db.ProductionOrders.AsNoTracking()
            .CountAsync(p =>
                p.ReleasedRoutingId != null
                && (p.Status == ProductionOrder.StatusReleased || p.Status == ProductionOrder.StatusInProgress),
                ct)
            .ConfigureAwait(false);

        var stagesStarted = await _db.ProductionOrderStageProgresses.AsNoTracking()
            .CountAsync(s =>
                s.StageStatus == ProductionOrderStageProgress.StageInProgress
                && s.UpdatedAt >= from && s.UpdatedAt <= to.AddDays(1), ct)
            .ConfigureAwait(false);
        var stagesCompleted = await _db.ProductionOrderStageProgresses.AsNoTracking()
            .CountAsync(s =>
                s.StageStatus == ProductionOrderStageProgress.StageCompleted
                && s.UpdatedAt >= from && s.UpdatedAt <= to.AddDays(1), ct)
            .ConfigureAwait(false);

        var produceBatches = await _db.GoodsProduceBatches.AsNoTracking()
            .CountAsync(b => b.GrDate >= from && b.GrDate <= to, ct)
            .ConfigureAwait(false);

        var grPending = await _db.GoodReceiptDocuments.AsNoTracking()
            .CountAsync(d => !d.IsPosted, ct)
            .ConfigureAwait(false);
        var grPosted = await _db.GoodReceiptDocuments.AsNoTracking()
            .CountAsync(d => d.IsPosted && d.PostedAt != null
                && d.PostedAt.Value.Date >= from && d.PostedAt.Value.Date <= to, ct)
            .ConfigureAwait(false);

        var qiPending = await _db.SalesReturnQualityInspections.AsNoTracking()
            .CountAsync(q => q.Status == SalesReturnQualityInspection.StatusPending, ct)
            .ConfigureAwait(false);
        var qiCompleted = await _db.SalesReturnQualityInspections.AsNoTracking()
            .CountAsync(q =>
                q.Status == SalesReturnQualityInspection.StatusCompleted
                && q.CompletedAt != null
                && q.CompletedAt.Value.Date >= from
                && q.CompletedAt.Value.Date <= to, ct)
            .ConfigureAwait(false);

        var recentPos = await _db.ProductionOrders.AsNoTracking()
            .OrderByDescending(p => p.PlannedStartDate).ThenByDescending(p => p.Id)
            .Take(5)
            .Select(p => new DashboardRecentProductionOrderRowVm
            {
                Id = p.Id,
                DocumentNumber = p.ProductionDocumentNumber ?? p.ProductionNumber.ToString(CultureInfo.InvariantCulture),
                MaterialNumber = p.FinishedMaterialNumber,
                Status = p.Status,
                TargetQuantity = p.TargetQuantity,
                PlannedStartDate = p.PlannedStartDate
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new ProductionModuleStats
        {
            MrpBomCount = fert,
            ProductionOrdersByStatus = map,
            OperationTrackingOpen = ot,
            ProductionOrdersByStatusChart = bySt
                .OrderBy(x => x.Key)
                .Select(x => new LabelCountDto { Label = x.Key, Count = x.C, Value = x.C })
                .ToList(),
            StagesStartedInPeriod = stagesStarted,
            StagesCompletedInPeriod = stagesCompleted,
            GoodsProduceBatchesInPeriod = produceBatches,
            ProductionGrPending = grPending,
            ProductionGrPostedInPeriod = grPosted,
            ReturnQiPending = qiPending,
            ReturnQiCompletedInPeriod = qiCompleted,
            RecentProductionOrders = recentPos
        };
    }

    private async Task<DashboardHeadlineVm> BuildHeadlineAsync(
        bool showSa, bool showSt, bool showPr,
        PeriodWindow current, DateTime prevFrom, DateTime prevTo,
        string? storePlant, DateTime today, CancellationToken ct)
    {
        decimal revenue = 0, revenuePrev = 0, outstanding = 0;
        int openCount = 0, ordersOpen = 0, ordersPendingPayment = 0, dcTransit = 0, lowStock = 0, wip = 0;
        IReadOnlyList<LabelCountDto> spark = Array.Empty<LabelCountDto>();

        if (showSa)
        {
            revenue = await _db.SalesInvoices.AsNoTracking()
                .Where(i => i.DocumentDate >= current.From && i.DocumentDate <= current.To)
                .SumAsync(i => (decimal?)i.GrandTotal, ct).ConfigureAwait(false) ?? 0m;
            revenuePrev = await _db.SalesInvoices.AsNoTracking()
                .Where(i => i.DocumentDate >= prevFrom && i.DocumentDate <= prevTo)
                .SumAsync(i => (decimal?)i.GrandTotal, ct).ConfigureAwait(false) ?? 0m;

            var invDates = await _db.SalesInvoices.AsNoTracking()
                .Where(i => i.DocumentDate >= current.From && i.DocumentDate <= current.To)
                .Select(i => new { i.DocumentDate, i.GrandTotal })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            spark = BuildDailyAmountSeries(
                current.From, current.To,
                invDates.Select(x => (x.DocumentDate, x.GrandTotal)));

            outstanding = await _db.SalesInvoices.AsNoTracking()
                .Where(i => i.Status == SalesInvoice.StatusOpen)
                .SumAsync(i => (decimal?)i.GrandTotal, ct).ConfigureAwait(false) ?? 0m;
            openCount = await _db.SalesInvoices.AsNoTracking()
                .CountAsync(i => i.Status == SalesInvoice.StatusOpen, ct).ConfigureAwait(false);
            var workflowCounts = await _orderWorkflow.CountByStatusAsync(null, ct).ConfigureAwait(false);
            ordersOpen = workflowCounts.GetValueOrDefault(SalesOrderWorkflowStatus.Open);
            ordersPendingPayment = workflowCounts.GetValueOrDefault(SalesOrderWorkflowStatus.PendingPayment);
            dcTransit = await _db.DeliveryChallans.AsNoTracking()
                .CountAsync(d => d.DeliveryCompletedAt == null, ct).ConfigureAwait(false);
        }

        if (showSt && !string.IsNullOrEmpty(storePlant))
        {
            var lines = await _db.StockInventoryLines.AsNoTracking()
                .Where(s => s.Status == StockInventoryLine.StatusActive && s.PlantID == storePlant)
                .Select(s => new { s.MaterialNumber, s.Quantity })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var qtyByMaterial = lines.GroupBy(s => s.MaterialNumber)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
            var reorderMaterials = await _db.CreateMaterialMaster.AsNoTracking()
                .Where(m => m.ReorderPoint != null && m.ReorderPoint > 0)
                .Select(m => new { m.MaterialNumber, m.ReorderPoint })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            lowStock = reorderMaterials.Count(m =>
                qtyByMaterial.TryGetValue(m.MaterialNumber, out var qty)
                    ? qty <= m.ReorderPoint!.Value
                    : true);
        }

        if (showPr)
        {
            wip = await _db.ProductionOrders.AsNoTracking()
                .CountAsync(p =>
                    p.Status == ProductionOrder.StatusReleased
                    || p.Status == ProductionOrder.StatusInProgress, ct)
                .ConfigureAwait(false);
        }

        return new DashboardHeadlineVm
        {
            ShowRevenue = showSa,
            RevenueInPeriod = revenue,
            RevenuePreviousPeriod = revenuePrev,
            RevenueSparkline = spark,
            ShowOutstandingAr = showSa,
            OutstandingAR = outstanding,
            OpenInvoiceCount = openCount,
            ShowOrdersFulfillment = showSa,
            OrdersOpen = ordersOpen,
            OrdersPendingPayment = ordersPendingPayment,
            DCsInTransit = dcTransit,
            ShowLowStock = showSt,
            LowStockCount = lowStock,
            ShowProductionWip = showPr,
            ProductionWip = wip
        };
    }

    private static List<LabelCountDto> BuildDailySeries(DateTime from, DateTime to, IEnumerable<DateTime> dates)
    {
        var lookup = dates.GroupBy(d => d.Date).ToDictionary(g => g.Key, g => g.Count());
        var list = new List<LabelCountDto>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            lookup.TryGetValue(d, out var c);
            list.Add(new LabelCountDto
            {
                Label = d.ToString("dd MMM", CultureInfo.InvariantCulture),
                Count = c,
                Value = c
            });
        }
        return list;
    }

    private static List<LabelCountDto> BuildDailyAmountSeries(
        DateTime from, DateTime to, IEnumerable<(DateTime Date, decimal Amount)> rows)
    {
        var lookup = rows
            .GroupBy(r => r.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        var list = new List<LabelCountDto>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            lookup.TryGetValue(d, out var amt);
            list.Add(new LabelCountDto
            {
                Label = d.ToString("dd MMM", CultureInfo.InvariantCulture),
                Count = 0,
                Value = amt
            });
        }
        return list;
    }

    private static string NormalizePeriod(string? period)
    {
        var p = (period ?? "").Trim().ToLowerInvariant();
        return p switch
        {
            DashboardPeriod.Today => DashboardPeriod.Today,
            DashboardPeriod.Last7d => DashboardPeriod.Last7d,
            DashboardPeriod.Last30d => DashboardPeriod.Last30d,
            _ => DashboardPeriod.Mtd
        };
    }

    private static PeriodWindow ResolvePeriodWindow(string periodKey)
    {
        var today = DateTime.Today;
        DateTime from, to, prevFrom, prevTo;
        string label;

        switch (periodKey)
        {
            case DashboardPeriod.Today:
                from = to = today;
                prevFrom = prevTo = today.AddDays(-1);
                label = "Today";
                break;
            case DashboardPeriod.Last7d:
                to = today;
                from = today.AddDays(-6);
                prevTo = from.AddDays(-1);
                prevFrom = prevTo.AddDays(-6);
                label = "Last 7 days";
                break;
            case DashboardPeriod.Last30d:
                to = today;
                from = today.AddDays(-29);
                prevTo = from.AddDays(-1);
                prevFrom = prevTo.AddDays(-29);
                label = "Last 30 days";
                break;
            default:
                from = new DateTime(today.Year, today.Month, 1);
                to = today;
                var days = (to - from).Days + 1;
                prevTo = from.AddDays(-1);
                prevFrom = prevTo.AddDays(-(days - 1));
                label = "Month to date";
                periodKey = DashboardPeriod.Mtd;
                break;
        }

        return new PeriodWindow(periodKey, label, from, to, prevFrom, prevTo);
    }

    private sealed record PeriodWindow(
        string Key, string Label, DateTime From, DateTime To, DateTime PreviousFrom, DateTime PreviousTo);

    public static string ChartJsonForLabelsValues(IReadOnlyList<string> labels, IReadOnlyList<int> values) =>
        JsonSerializer.Serialize(new { labels, datasets = new[] { new { label = "Count", data = values, backgroundColor = "rgba(0, 112, 242, 0.5)", borderColor = "rgb(0, 112, 242)", borderWidth = 1 } } });

    public static string ChartJsonDoughnut(IReadOnlyList<LabelCountDto> items, IReadOnlyList<string>? background = null) =>
        JsonSerializer.Serialize(new
        {
            labels = items.Select(i => i.Label).ToList(),
            datasets = new[] { new { data = items.Select(i => (double)i.Value).ToList(), backgroundColor = background ?? new[] { "#0070f2", "#27ae60", "#9b59b6", "#e67e22", "#e74c3c", "#95a5a6" } } }
        });

    /// <summary>Doughnut chart for sales order workflow — one distinct color per status label.</summary>
    public static string ChartJsonSalesOrderWorkflowDoughnut(IReadOnlyList<LabelCountDto> items)
    {
        var colorByStatus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SalesOrderWorkflowStatus.Open] = "#94a3b8",
            [SalesOrderWorkflowStatus.PendingStock] = "#dc2626",
            [SalesOrderWorkflowStatus.PendingGoodReceive] = "#ca8a04",
            [SalesOrderWorkflowStatus.PendingDc] = "#0891b2",
            [SalesOrderWorkflowStatus.DeliveryInProcess] = "#2563eb",
            [SalesOrderWorkflowStatus.PendingPayment] = "#ea580c",
            [SalesOrderWorkflowStatus.Completed] = "#16a34a"
        };
        var colors = items
            .Select(i => colorByStatus.TryGetValue(i.Label, out var c) ? c : "#64748b")
            .ToList();
        return ChartJsonDoughnut(items, colors);
    }

    public static string ChartJsonLineQuotationsAndOrders(IReadOnlyList<LabelCountDto> quotations, IReadOnlyList<LabelCountDto> orders) =>
        JsonSerializer.Serialize(new
        {
            labels = quotations.Select(x => x.Label).ToList(),
            datasets = new[]
            {
                new
                {
                    label = "Quotations",
                    data = quotations.Select(x => (double)x.Count).ToList(),
                    borderColor = "rgb(0, 112, 242)",
                    backgroundColor = "rgba(0, 112, 242, 0.12)",
                    tension = 0.25,
                    fill = true
                },
                new
                {
                    label = "Sales orders",
                    data = orders.Select(x => (double)x.Count).ToList(),
                    borderColor = "rgb(39, 174, 96)",
                    backgroundColor = "rgba(39, 174, 96, 0.12)",
                    tension = 0.25,
                    fill = true
                }
            }
        });

    public static string ChartJsonInvoicedVsPayments(
        IReadOnlyList<LabelCountDto> invoiced, IReadOnlyList<LabelCountDto> payments)
    {
        var labels = invoiced.Select(x => x.Label).ToList();
        return JsonSerializer.Serialize(new
        {
            labels,
            datasets = new[]
            {
                new
                {
                    label = "Invoices billed",
                    data = invoiced.Select(x => (double)x.Value).ToList(),
                    borderColor = "rgb(0, 112, 242)",
                    backgroundColor = "rgba(0, 112, 242, 0.12)",
                    tension = 0.25,
                    fill = true
                },
                new
                {
                    label = "Payments collected",
                    data = AlignSeriesToLabels(labels, payments).Select(x => (double)x).ToList(),
                    borderColor = "rgb(39, 174, 96)",
                    backgroundColor = "rgba(39, 174, 96, 0.12)",
                    tension = 0.25,
                    fill = true
                }
            }
        });
    }

    private static IReadOnlyList<decimal> AlignSeriesToLabels(
        IReadOnlyList<string> labels, IReadOnlyList<LabelCountDto> series)
    {
        var map = series.ToDictionary(x => x.Label, x => x.Value);
        return labels.Select(l => map.TryGetValue(l, out var v) ? v : 0m).ToList();
    }

    public static string ChartJsonSparkline(IReadOnlyList<LabelCountDto> points, string color = "rgb(0, 112, 242)") =>
        JsonSerializer.Serialize(new
        {
            labels = points.Select(p => p.Label).ToList(),
            datasets = new[]
            {
                new
                {
                    data = points.Select(p => (double)p.Value).ToList(),
                    borderColor = color,
                    backgroundColor = "rgba(0, 112, 242, 0.15)",
                    borderWidth = 2,
                    pointRadius = 0,
                    tension = 0.35,
                    fill = true
                }
            }
        });
}
