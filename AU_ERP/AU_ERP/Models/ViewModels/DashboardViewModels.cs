namespace AU_ERP.Models.ViewModels;

/// <summary>Main dashboard: KPIs and chart payloads per module; sections shown by department access.</summary>
public sealed class DashboardPageVm
{
    public string UserDisplayName { get; init; } = "";
    public IReadOnlyList<string> DepartmentNames { get; init; } = Array.Empty<string>();
    public string TodayLabel { get; init; } = "";

    public string Period { get; init; } = DashboardPeriod.Mtd;
    public string PeriodLabel { get; init; } = "";
    public DateTime PeriodFrom { get; init; }
    public DateTime PeriodTo { get; init; }

    public bool ShowAdmin { get; init; }
    public bool ShowStore { get; init; }
    public bool ShowSales { get; init; }
    public bool ShowProduction { get; init; }
    public bool HasAnyModule { get; init; }

    public DashboardHeadlineVm Headline { get; init; } = new();

    public AdminModuleStats? Admin { get; init; }
    public StoreModuleStats? Store { get; init; }
    public SalesModuleStats? Sales { get; init; }
    public ProductionModuleStats? Production { get; init; }
}

public static class DashboardPeriod
{
    public const string Today = "today";
    public const string Last7d = "7d";
    public const string Last30d = "30d";
    public const string Mtd = "mtd";

    public static readonly IReadOnlyList<(string Key, string Label)> Options = new[]
    {
        (Today, "Today"),
        (Last7d, "Last 7 days"),
        (Last30d, "Last 30 days"),
        (Mtd, "Month to date")
    };
}

public sealed class DashboardHeadlineVm
{
    public bool ShowRevenue { get; init; }
    public decimal RevenueInPeriod { get; init; }
    public decimal RevenuePreviousPeriod { get; init; }
    public IReadOnlyList<LabelCountDto> RevenueSparkline { get; init; } = Array.Empty<LabelCountDto>();

    public bool ShowOutstandingAr { get; init; }
    public decimal OutstandingAR { get; init; }
    public int OpenInvoiceCount { get; init; }

    public bool ShowOrdersFulfillment { get; init; }
    public int OrdersOpen { get; init; }
    public int DCsInTransit { get; init; }

    public bool ShowLowStock { get; init; }
    public int LowStockCount { get; init; }

    public bool ShowProductionWip { get; init; }
    public int ProductionWip { get; init; }
}

public sealed class AdminModuleStats
{
    public int MaterialCount { get; init; }
    public int BusinessPartnerCount { get; init; }
    public int BomCount { get; init; }
    public int WorkCentreCount { get; init; }
    public int RoutingCount { get; init; }
    public int UserCount { get; init; }
    public int DriverCount { get; init; }
    public int VehicleCount { get; init; }
    public int DocumentRangeCount { get; init; }
    public int DocumentIntegrationCount { get; init; }
    public IReadOnlyList<string> BarLabels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<int> BarValues { get; init; } = Array.Empty<int>();
    public IReadOnlyList<LabelCountDto> MaterialByType { get; init; } = Array.Empty<LabelCountDto>();
}

public sealed class StoreModuleStats
{
    public int ActiveStockLineCount { get; init; }
    public int DistinctMaterialCount { get; init; }
    public decimal TotalStockValue { get; init; }
    public int LowStockCount { get; init; }
    public int StockMovementsInPeriod { get; init; }
    public int GoodsReceiptDocsInPeriod { get; init; }
    public int GoodsIssueDocsInPeriod { get; init; }
    public IReadOnlyList<LabelCountDto> ValueByGrade { get; init; } = Array.Empty<LabelCountDto>();
    public IReadOnlyList<LabelCountDto> MovementsByDay { get; init; } = Array.Empty<LabelCountDto>();
    public IReadOnlyList<DashboardTopStockRowVm> TopStockByValue { get; init; } = Array.Empty<DashboardTopStockRowVm>();
}

public sealed class DashboardTopStockRowVm
{
    public string MaterialNumber { get; init; } = "";
    public string? MaterialDescription { get; init; }
    public string Grade { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal StockValue { get; init; }
}

public sealed class SalesModuleStats
{
    // Pre-Sales
    public int QuotationDraft { get; init; }
    public int QuotationSent { get; init; }
    public int QuotationsCreatedInPeriod { get; init; }
    public decimal? QuotationToOrderConversionPercent { get; init; }
    public IReadOnlyList<LabelCountDto> QuotationByMonth { get; init; } = Array.Empty<LabelCountDto>();
    public IReadOnlyList<LabelCountDto> SalesOrderByMonth { get; init; } = Array.Empty<LabelCountDto>();

    // Order & Fulfillment
    public int OrderOpen { get; init; }
    public int OrderConfirmed { get; init; }
    public int DeliveryChallanCount { get; init; }
    public int DeliveryChallansCreatedInPeriod { get; init; }
    public int DeliveryChallansDelivered { get; init; }
    public int DeliveryChallansInTransit { get; init; }
    public int SalesGoodsIssuePendingInPeriod { get; init; }
    public int SalesGoodsIssueReceivedInPeriod { get; init; }
    public IReadOnlyList<DashboardRecentDcRowVm> RecentDeliveryChallans { get; init; } = Array.Empty<DashboardRecentDcRowVm>();

    // Billing & Payments
    public int InvoiceOpenCount { get; init; }
    public int InvoiceOverdueCount { get; init; }
    public int InvoiceCollectedCount { get; init; }
    public int InvoiceReturnInProcessCount { get; init; }
    public int InvoiceReturnedCount { get; init; }
    public decimal InvoiceOpenAmount { get; init; }
    public decimal InvoiceOutstandingAmount { get; init; }
    public decimal InvoiceCollectedAmountInPeriod { get; init; }
    public int CreditMemosInPeriod { get; init; }
    public decimal CreditMemosAmountInPeriod { get; init; }
    public int PaymentsInPeriod { get; init; }
    public decimal PaymentsAmountInPeriod { get; init; }
    public int ReturnOrdersOpenCount { get; init; }
    public int ReturnQiPendingCount { get; init; }
    public IReadOnlyList<LabelCountDto> InvoicedVsPaymentsByDay { get; init; } = Array.Empty<LabelCountDto>();
    public IReadOnlyList<LabelCountDto> PaymentsCollectedByDay { get; init; } = Array.Empty<LabelCountDto>();
    public IReadOnlyList<DashboardOverdueInvoiceRowVm> RecentOverdueInvoices { get; init; } = Array.Empty<DashboardOverdueInvoiceRowVm>();
}

public sealed class DashboardRecentDcRowVm
{
    public int Id { get; init; }
    public string DocumentNumber { get; init; } = "";
    public string? DealerDisplayName { get; init; }
    public string StatusLabel { get; init; } = "";
    public DateTime DocumentDate { get; init; }
}

public sealed class DashboardOverdueInvoiceRowVm
{
    public int Id { get; init; }
    public string DocumentNumber { get; init; } = "";
    public string? DealerDisplayName { get; init; }
    public DateTime DueDate { get; init; }
    public int DaysOverdue { get; init; }
    public decimal Balance { get; init; }
}

public sealed class ProductionModuleStats
{
    public int MrpBomCount { get; init; }
    public IReadOnlyDictionary<string, int> ProductionOrdersByStatus { get; init; } = new Dictionary<string, int>();
    public int OperationTrackingOpen { get; init; }
    public IReadOnlyList<LabelCountDto> ProductionOrdersByStatusChart { get; init; } = Array.Empty<LabelCountDto>();
    public int StagesStartedInPeriod { get; init; }
    public int StagesCompletedInPeriod { get; init; }
    public int GoodsProduceBatchesInPeriod { get; init; }
    public int ProductionGrPending { get; init; }
    public int ProductionGrPostedInPeriod { get; init; }
    public int ReturnQiPending { get; init; }
    public int ReturnQiCompletedInPeriod { get; init; }
    public IReadOnlyList<DashboardRecentProductionOrderRowVm> RecentProductionOrders { get; init; } = Array.Empty<DashboardRecentProductionOrderRowVm>();
}

public sealed class DashboardRecentProductionOrderRowVm
{
    public int Id { get; init; }
    public string DocumentNumber { get; init; } = "";
    public string MaterialNumber { get; init; } = "";
    public string Status { get; init; } = "";
    public int TargetQuantity { get; init; }
    public DateTime PlannedStartDate { get; init; }
}

public sealed class LabelCountDto
{
    public string Label { get; init; } = "";
    public decimal Value { get; init; }
    public int Count { get; init; }
}
