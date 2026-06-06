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

    public async Task<DashboardPageVm> BuildAsync(ClaimsPrincipal user, string? period = null, string? plantId = null, CancellationToken ct = default)
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
        var showFi = user.HasClaim(AuClaimTypes.Department, "Finance");
        var showPr = user.HasClaim(AuClaimTypes.Department, "Production");
        var hasAny = showAd || showSt || showSa || showFi || showPr;

        var periodKey = NormalizePeriod(period);
        var window = ResolvePeriodWindow(periodKey);
        var today = DateTime.Today;
        var plantScope = await ResolveDashboardPlantScopeAsync(user, plantId, ct).ConfigureAwait(false);
        var selectedPlantId = plantScope.EffectiveListPlantId;

        AdminModuleStats? admin = null;
        if (showAd)
            admin = await BuildAdminAsync(ct).ConfigureAwait(false);

        StoreModuleStats? store = null;
        if (showSt)
        {
            store = await BuildStoreAsync(plantScope, window.From, window.To, ct).ConfigureAwait(false);
        }

        SalesModuleStats? sales = null;
        if (showSa)
            sales = await BuildSalesAsync(plantScope, window.From, window.To, today, ct).ConfigureAwait(false);

        FinanceModuleStats? finance = null;
        if (showFi)
            finance = await BuildFinanceAsync(window.From, window.To, today, ct).ConfigureAwait(false);

        ProductionModuleStats? prod = null;
        if (showPr)
            prod = await BuildProductionAsync(plantScope, window.From, window.To, ct).ConfigureAwait(false);

        var headline = await BuildHeadlineAsync(
            showSa, showFi, showSt, showPr,
            window, window.PreviousFrom, window.PreviousTo,
            plantScope,
            today, ct).ConfigureAwait(false);

        var plantOptions = await BuildDashboardPlantOptionsAsync(plantScope, ct).ConfigureAwait(false);
        var selectedPlantName = ResolvePlantName(plantOptions, selectedPlantId);

        return new DashboardPageVm
        {
            UserDisplayName = display,
            DepartmentNames = deptNames,
            TodayLabel = today.ToString("dddd, dd MMM yyyy", CultureInfo.InvariantCulture),
            Period = periodKey,
            PeriodLabel = window.Label,
            PeriodFrom = window.From,
            PeriodTo = window.To,
            SelectedPlantId = selectedPlantId,
            IsPlantFilterVisible = plantOptions.Count > 1,
            IsPlantSingleLocked = plantScope.IsSinglePlantLocked,
            EffectivePlantName = selectedPlantName,
            PlantOptions = plantOptions,
            ShowAdmin = showAd,
            ShowStore = showSt,
            ShowSales = showSa,
            ShowFinance = showFi,
            ShowProduction = showPr,
            HasAnyModule = hasAny,
            Headline = headline,
            Admin = admin,
            Store = store,
            Sales = sales,
            Finance = finance,
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
        SalesPlantScope scope, DateTime from, DateTime to, CancellationToken ct)
    {
        if (scope.MissingAssignment)
        {
            return new StoreModuleStats
            {
                ValueByGrade = new List<LabelCountDto>(),
                MovementsByDay = BuildDailySeries(from, to, Array.Empty<DateTime>()),
                TopStockByValue = new List<DashboardTopStockRowVm>()
            };
        }

        var stockQuery = SalesPlantAccess.ApplyListingPlantFilter(
            _db.StockInventoryLines.AsNoTracking()
                .Where(s => s.Status == StockInventoryLine.StatusActive),
            scope,
            s => s.PlantID);
        var lines = await stockQuery
            .Include(s => s.Material)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byGrade = lines
            .GroupBy(s => StockInventoryGradeCodes.NormalizeGradeKey(s.Grade))
            .Select(g => new LabelCountDto
            {
                Label = StockInventoryGradeCodes.DisplayGradeLabel(g.Key),
                Count = (int)Math.Round(g.Sum(x => x.Quantity), MidpointRounding.AwayFromZero),
                Value = g.Sum(x => x.StockValue)
            })
            .Where(x => x.Count > 0 || x.Value > 0)
            .OrderByDescending(x => x.Value > 0 ? x.Value : x.Count)
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
        if (!scope.IsAdminAllPlants || !string.IsNullOrWhiteSpace(scope.EffectiveListPlantId))
        {
            var allowedPlants = scope.EffectiveListPlantId != null
                ? new[] { scope.EffectiveListPlantId }
                : scope.AllowedPlantIds;
            movementsQ = movementsQ.Where(m =>
                (m.FromPlantId != null && allowedPlants.Contains(m.FromPlantId))
                || (m.ToPlantId != null && allowedPlants.Contains(m.ToPlantId)));
        }

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
            .GroupBy(s => new
            {
                s.MaterialNumber,
                MaterialDescription = s.Material != null ? s.Material.Description : null,
                Grade = StockInventoryGradeCodes.DisplayGradeLabel(s.Grade)
            })
            .Select(g => new DashboardTopStockRowVm
            {
                MaterialNumber = g.Key.MaterialNumber,
                MaterialDescription = g.Key.MaterialDescription,
                Grade = g.Key.Grade,
                Quantity = g.Sum(x => x.Quantity),
                StockValue = g.Sum(x => x.StockValue),
                BatchBreakdown = g
                    .GroupBy(x => string.IsNullOrWhiteSpace(x.BatchOrLot) ? "—" : x.BatchOrLot!)
                    .OrderBy(bg => bg.Key)
                    .Select(bg => new DashboardTopStockBatchRowVm
                    {
                        BatchOrLot = bg.Key,
                        Quantity = bg.Sum(x => x.Quantity),
                        StockValue = bg.Sum(x => x.StockValue)
                    })
                    .ToList()
            })
            .OrderByDescending(s => s.StockValue)
            .ThenBy(s => s.MaterialNumber)
            .ThenBy(s => s.Grade)
            .Take(5)
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

    private static SalesOrderWorkflowCountFilter? BuildSalesWorkflowCountFilter(SalesPlantScope scope)
    {
        if (scope.MissingAssignment)
            return new SalesOrderWorkflowCountFilter { PlantId = "\0__none__" };
        if (scope.IsAdminAllPlants)
            return null;
        if (scope.IsSinglePlantLocked || !string.IsNullOrWhiteSpace(scope.EffectiveListPlantId))
            return new SalesOrderWorkflowCountFilter
            {
                PlantId = scope.EffectiveListPlantId ?? scope.AllowedPlantIds[0]
            };
        return new SalesOrderWorkflowCountFilter { AllowedPlantIds = scope.AllowedPlantIds };
    }

    private async Task<SalesModuleStats> BuildSalesAsync(
        SalesPlantScope scope,
        DateTime from, DateTime to, DateTime today, CancellationToken ct)
    {
        if (scope.MissingAssignment)
        {
            return new SalesModuleStats
            {
                QuotationByMonth = EmptyMonthSeries(today),
                SalesOrderByMonth = EmptyMonthSeries(today),
                OrderWorkflowByStatus = new List<LabelCountDto>()
            };
        }

        IQueryable<SalesQuotation> quotQuery = _db.SalesQuotations.AsNoTracking();
        quotQuery = SalesPlantAccess.ApplyListingPlantFilter(quotQuery, scope, x => x.PlantId);
        IQueryable<SalesOrder> orderQuery = _db.SalesOrders.AsNoTracking();
        orderQuery = SalesPlantAccess.ApplyListingPlantFilter(orderQuery, scope, x => x.PlantId);
        IQueryable<DeliveryChallan> dcQuery = _db.DeliveryChallans.AsNoTracking();
        dcQuery = SalesPlantAccess.ApplyListingPlantFilter(dcQuery, scope, x => x.PlantId);

        var qDraft = await quotQuery
            .CountAsync(x => x.Status == SalesQuotation.StatusDraft, ct).ConfigureAwait(false);
        var qSent = await quotQuery
            .CountAsync(x => x.Status == SalesQuotation.StatusSent, ct).ConfigureAwait(false);
        var workflowCounts = await _orderWorkflow.CountByStatusAsync(
            BuildSalesWorkflowCountFilter(scope), ct).ConfigureAwait(false);
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
        var dcTotal = await dcQuery.CountAsync(ct).ConfigureAwait(false);

        var qInPeriod = await quotQuery
            .CountAsync(x => x.QuotationDate >= from && x.QuotationDate <= to, ct).ConfigureAwait(false);
        var oInPeriod = await orderQuery
            .CountAsync(x => x.OrderDate >= from && x.OrderDate <= to, ct).ConfigureAwait(false);
        decimal? conversion = qInPeriod > 0
            ? Math.Round((decimal)oInPeriod / qInPeriod * 100m, 1)
            : null;

        var trendFrom = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        var quots = await quotQuery
            .Where(x => x.QuotationDate >= trendFrom)
            .Select(x => x.QuotationDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var orders = await orderQuery
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

        var dcCreated = await dcQuery
            .CountAsync(d => d.DocumentDate >= from && d.DocumentDate <= to, ct).ConfigureAwait(false);
        var dcDelivered = await dcQuery
            .CountAsync(d => d.DeliveryCompletedAt != null, ct).ConfigureAwait(false);
        var dcInTransit = await dcQuery
            .CountAsync(d => d.DeliveryCompletedAt == null, ct).ConfigureAwait(false);

        var sgiJoined = from doc in _db.SalesGoodsIssueDocuments.AsNoTracking()
            join so in _db.SalesOrders.AsNoTracking() on doc.SalesOrderId equals so.Id
            select new { doc, PlantId = so.PlantId };
        sgiJoined = SalesPlantAccess.ApplyListingPlantFilter(sgiJoined, scope, x => x.PlantId);
        var sgiPending = await sgiJoined
            .CountAsync(d =>
                d.doc.DocumentDate >= from && d.doc.DocumentDate <= to
                && d.doc.Status == SalesGoodsIssueDocument.StatusPending, ct)
            .ConfigureAwait(false);
        var sgiReceived = await sgiJoined
            .CountAsync(d =>
                d.doc.DocumentDate >= from && d.doc.DocumentDate <= to
                && d.doc.Status == SalesGoodsIssueDocument.StatusReceived, ct)
            .ConfigureAwait(false);

        var recentDcs = await dcQuery
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

        var roJoined = from ro in _db.SalesReturnOrders.AsNoTracking()
            join inv in _db.SalesInvoices.AsNoTracking() on ro.SalesInvoiceId equals inv.Id
            join dc in _db.DeliveryChallans.AsNoTracking() on inv.DeliveryChallanId equals dc.Id
            where !_db.SalesReturnCreditMemos.Any(c => c.SalesReturnOrderId == ro.Id)
            select new { ro, PlantId = dc.PlantId };
        roJoined = SalesPlantAccess.ApplyListingPlantFilter(roJoined, scope, x => x.PlantId);
        var roOpen = await roJoined.CountAsync(ct).ConfigureAwait(false);

        IQueryable<SalesReturnQualityInspection> qiQuery = _db.SalesReturnQualityInspections.AsNoTracking()
            .Where(q => q.Status == SalesReturnQualityInspection.StatusPending);
        qiQuery = SalesPlantAccess.ApplyListingPlantFilter(qiQuery, scope, q => q.PlantId);
        var qiPending = await qiQuery.CountAsync(ct).ConfigureAwait(false);

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
            ReturnOrdersOpenCount = roOpen,
            ReturnQiPendingCount = qiPending
        };
    }

    private async Task<FinanceModuleStats> BuildFinanceAsync(
        DateTime from, DateTime to, DateTime today, CancellationToken ct)
    {
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

        return new FinanceModuleStats
        {
            InvoiceOpenCount = invOpenCount,
            InvoiceOverdueCount = invOverdueCount,
            InvoiceCollectedCount = invCollectedCount,
            InvoiceReturnInProcessCount = invRipCount,
            InvoiceReturnedCount = invReturnedCount,
            InvoiceOpenAmount = openAmount,
            InvoiceOutstandingAmount = openAmount,
            InvoiceCollectedAmountInPeriod = collectedInPeriod,
            CreditMemosInPeriod = cmInPeriod.Count,
            CreditMemosAmountInPeriod = cmInPeriod.Sum(c => c.GrandTotalCredit),
            PaymentsInPeriod = paymentsInPeriod,
            PaymentsAmountInPeriod = collectedInPeriod,
            InvoicedVsPaymentsByDay = invoicedByDay,
            PaymentsCollectedByDay = paymentsByDay,
            RecentOverdueInvoices = overdueRows
        };
    }

    private async Task<ProductionModuleStats> BuildProductionAsync(
        SalesPlantScope scope, DateTime from, DateTime to, CancellationToken ct)
    {
        if (scope.MissingAssignment)
        {
            return new ProductionModuleStats
            {
                ProductionOrdersByStatus = new Dictionary<string, int>(),
                ProductionOrdersByStatusChart = new List<LabelCountDto>(),
                RecentProductionOrders = new List<DashboardRecentProductionOrderRowVm>()
            };
        }

        var orderQuery = SalesPlantAccess.ApplyProductionOrderPlantFilter(
            _db.ProductionOrders.AsNoTracking(),
            scope);

        var bySt = await orderQuery
            .GroupBy(p => p.Status)
            .Select(g => new { g.Key, C = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var map = bySt.ToDictionary(x => x.Key, x => x.C);
        var fert = await _db.CreateMaterialMaster.AsNoTracking()
            .CountAsync(m => m.MaterialTypeCode == "FERT" || m.MaterialTypeCode == "HALB", ct)
            .ConfigureAwait(false);
        var ot = await orderQuery
            .CountAsync(p =>
                p.ReleasedRoutingId != null
                && (p.Status == ProductionOrder.StatusReleased || p.Status == ProductionOrder.StatusInProgress),
                ct)
            .ConfigureAwait(false);

        var stageQuery = _db.ProductionOrderStageProgresses.AsNoTracking();
        if (!scope.IsAdminAllPlants || !string.IsNullOrWhiteSpace(scope.EffectiveListPlantId))
        {
            var allowedPlants = scope.EffectiveListPlantId != null
                ? new[] { scope.EffectiveListPlantId }
                : scope.AllowedPlantIds;
            stageQuery = stageQuery.Where(s => s.ProductionOrder!.Lines
                .Any(l => l.PlantId != null && allowedPlants.Contains(l.PlantId)));
        }

        var stagesStarted = await stageQuery
            .CountAsync(s =>
                s.StageStatus == ProductionOrderStageProgress.StageInProgress
                && s.UpdatedAt >= from && s.UpdatedAt <= to.AddDays(1), ct)
            .ConfigureAwait(false);
        var stagesCompleted = await stageQuery
            .CountAsync(s =>
                s.StageStatus == ProductionOrderStageProgress.StageCompleted
                && s.UpdatedAt >= from && s.UpdatedAt <= to.AddDays(1), ct)
            .ConfigureAwait(false);

        var batchQuery = _db.GoodsProduceBatches.AsNoTracking()
            .Where(b => b.GrDate >= from && b.GrDate <= to);
        if (!scope.IsAdminAllPlants || !string.IsNullOrWhiteSpace(scope.EffectiveListPlantId))
        {
            var allowedPlants = scope.EffectiveListPlantId != null
                ? new[] { scope.EffectiveListPlantId }
                : scope.AllowedPlantIds;
            batchQuery = batchQuery.Where(b => b.ProductionOrderLine != null
                && b.ProductionOrderLine.PlantId != null
                && allowedPlants.Contains(b.ProductionOrderLine.PlantId));
        }
        var produceBatches = await batchQuery
            .CountAsync(ct)
            .ConfigureAwait(false);

        var grQuery = _db.GoodReceiptDocuments.AsNoTracking();
        if (!scope.IsAdminAllPlants || !string.IsNullOrWhiteSpace(scope.EffectiveListPlantId))
        {
            var allowedPlants = scope.EffectiveListPlantId != null
                ? new[] { scope.EffectiveListPlantId }
                : scope.AllowedPlantIds;
            grQuery = grQuery.Where(d => d.ProductionOrder != null
                && d.ProductionOrder.Lines.Any(l => l.PlantId != null && allowedPlants.Contains(l.PlantId)));
        }

        var grPending = await grQuery
            .CountAsync(d => !d.IsPosted, ct)
            .ConfigureAwait(false);
        var grPosted = await grQuery
            .CountAsync(d => d.IsPosted && d.PostedAt != null
                && d.PostedAt.Value.Date >= from && d.PostedAt.Value.Date <= to, ct)
            .ConfigureAwait(false);

        IQueryable<SalesReturnQualityInspection> qiQuery = _db.SalesReturnQualityInspections.AsNoTracking();
        qiQuery = SalesPlantAccess.ApplyListingPlantFilter(qiQuery, scope, q => q.PlantId);
        var qiPending = await qiQuery
            .CountAsync(q => q.Status == SalesReturnQualityInspection.StatusPending, ct)
            .ConfigureAwait(false);
        var qiCompleted = await qiQuery
            .CountAsync(q =>
                q.Status == SalesReturnQualityInspection.StatusCompleted
                && q.CompletedAt != null
                && q.CompletedAt.Value.Date >= from
                && q.CompletedAt.Value.Date <= to, ct)
            .ConfigureAwait(false);

        var recentPos = await orderQuery
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

    private static List<LabelCountDto> EmptyMonthSeries(DateTime today)
    {
        var trendFrom = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        var list = new List<LabelCountDto>();
        for (var i = 0; i < 6; i++)
        {
            var t = trendFrom.AddMonths(i);
            list.Add(new LabelCountDto
            {
                Label = t.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                Count = 0,
                Value = 0
            });
        }
        return list;
    }

    private async Task<DashboardHeadlineVm> BuildHeadlineAsync(
        bool showSa, bool showFi, bool showSt, bool showPr,
        PeriodWindow current, DateTime prevFrom, DateTime prevTo,
        SalesPlantScope scope, DateTime today, CancellationToken ct)
    {
        decimal revenue = 0, revenuePrev = 0, outstanding = 0;
        int openCount = 0, ordersOpen = 0, ordersPendingPayment = 0, dcTransit = 0, lowStock = 0, wip = 0;
        IReadOnlyList<LabelCountDto> spark = Array.Empty<LabelCountDto>();

        if (showFi)
        {
            var financeInvoiceCurrent = SalesPlantAccess.ApplyListingPlantFilter(
                _db.SalesInvoices.AsNoTracking()
                    .Where(i => i.DocumentDate >= current.From && i.DocumentDate <= current.To),
                scope,
                i => i.DeliveryChallan != null ? i.DeliveryChallan.PlantId : null);
            var financeInvoicePrev = SalesPlantAccess.ApplyListingPlantFilter(
                _db.SalesInvoices.AsNoTracking()
                    .Where(i => i.DocumentDate >= prevFrom && i.DocumentDate <= prevTo),
                scope,
                i => i.DeliveryChallan != null ? i.DeliveryChallan.PlantId : null);
            revenue = await financeInvoiceCurrent
                .SumAsync(i => (decimal?)i.GrandTotal, ct).ConfigureAwait(false) ?? 0m;
            revenuePrev = await financeInvoicePrev
                .SumAsync(i => (decimal?)i.GrandTotal, ct).ConfigureAwait(false) ?? 0m;

            var invDates = await financeInvoiceCurrent
                .Select(i => new { i.DocumentDate, i.GrandTotal })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            spark = BuildDailyAmountSeries(
                current.From, current.To,
                invDates.Select(x => (x.DocumentDate, x.GrandTotal)));

            var openInvoiceQuery = SalesPlantAccess.ApplyListingPlantFilter(
                _db.SalesInvoices.AsNoTracking()
                    .Where(i => i.Status == SalesInvoice.StatusOpen),
                scope,
                i => i.DeliveryChallan != null ? i.DeliveryChallan.PlantId : null);
            outstanding = await openInvoiceQuery
                .SumAsync(i => (decimal?)i.GrandTotal, ct).ConfigureAwait(false) ?? 0m;
            openCount = await openInvoiceQuery
                .CountAsync(ct).ConfigureAwait(false);
        }

        if (showSa)
        {
            if (!scope.MissingAssignment)
            {
                var workflowCounts = await _orderWorkflow.CountByStatusAsync(
                    BuildSalesWorkflowCountFilter(scope), ct).ConfigureAwait(false);
                ordersOpen = workflowCounts.GetValueOrDefault(SalesOrderWorkflowStatus.Open);
                ordersPendingPayment = workflowCounts.GetValueOrDefault(SalesOrderWorkflowStatus.PendingPayment);
                var dcQuery = SalesPlantAccess.ApplyListingPlantFilter(
                    _db.DeliveryChallans.AsNoTracking(), scope, d => d.PlantId);
                dcTransit = await dcQuery
                    .CountAsync(d => d.DeliveryCompletedAt == null, ct).ConfigureAwait(false);
            }
        }

        if (showSt && !scope.MissingAssignment)
        {
            var stockLines = SalesPlantAccess.ApplyListingPlantFilter(
                _db.StockInventoryLines.AsNoTracking()
                    .Where(s => s.Status == StockInventoryLine.StatusActive),
                scope,
                s => s.PlantID);
            var lines = await stockLines
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
            var wipQuery = SalesPlantAccess.ApplyProductionOrderPlantFilter(
                _db.ProductionOrders.AsNoTracking(),
                scope);
            wip = await wipQuery
                .CountAsync(p =>
                    p.Status == ProductionOrder.StatusReleased
                    || p.Status == ProductionOrder.StatusInProgress, ct)
                .ConfigureAwait(false);
        }

        return new DashboardHeadlineVm
        {
            ShowRevenue = showFi,
            RevenueInPeriod = revenue,
            RevenuePreviousPeriod = revenuePrev,
            RevenueSparkline = spark,
            ShowOutstandingAr = showFi,
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

    private async Task<SalesPlantScope> ResolveDashboardPlantScopeAsync(
        ClaimsPrincipal user,
        string? requestedPlantId,
        CancellationToken ct)
    {
        var allPlantIds = await _db.PlantsSamples.AsNoTracking()
            .Select(p => p.PlantID)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return await SalesPlantAccess.ResolveAsync(_db, user, requestedPlantId, allPlantIds, ct)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<DashboardPlantOptionVm>> BuildDashboardPlantOptionsAsync(
        SalesPlantScope scope, CancellationToken ct)
    {
        if (scope.MissingAssignment)
            return Array.Empty<DashboardPlantOptionVm>();

        var allowed = scope.AllowedPlantIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var plants = await _db.PlantsSamples.AsNoTracking()
            .Where(p => scope.IsAdminAllPlants || allowed.Contains(p.PlantID))
            .OrderBy(p => p.PlantName)
            .Select(p => new DashboardPlantOptionVm
            {
                PlantId = p.PlantID,
                PlantName = string.IsNullOrWhiteSpace(p.PlantName) ? p.PlantID : p.PlantName!
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return plants;
    }

    private static string? ResolvePlantName(IReadOnlyList<DashboardPlantOptionVm> options, string? selectedPlantId)
    {
        if (string.IsNullOrWhiteSpace(selectedPlantId))
            return null;
        return options.FirstOrDefault(p =>
            string.Equals(p.PlantId, selectedPlantId, StringComparison.OrdinalIgnoreCase))?.PlantName;
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

    /// <summary>Doughnut chart using quantity (Count) with stock value carried for tooltips.</summary>
    public static string ChartJsonGradeQuantityDoughnut(IReadOnlyList<LabelCountDto> items, IReadOnlyList<string>? background = null) =>
        JsonSerializer.Serialize(new
        {
            labels = items.Select(i => i.Label).ToList(),
            stockValues = items.Select(i => (double)i.Value).ToList(),
            datasets = new[] { new { data = items.Select(i => (double)i.Count).ToList(), backgroundColor = background ?? new[] { "#0070f2", "#27ae60", "#9b59b6", "#e67e22", "#e74c3c", "#95a5a6" } } }
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
