namespace AU_ERP.Models.Mobile;

// ─── Generic wrappers ─────────────────────────────────────────────────────────

public record MobileApiResponse<T>(bool Success, string? Message, T? Data)
{
    public static MobileApiResponse<T> Ok(T data) => new(true, null, data);
    public static MobileApiResponse<T> Fail(string message) => new(false, message, default);
}

public record PagedResult<T>(List<T> Rows, int Page, int PageSize, int Total);

// ─── Auth ─────────────────────────────────────────────────────────────────────

public record MobileLoginRequest(string Email, string Password);

public record MobileLoginResponse(
    string Token,
    DateTime ExpiresAt,
    string UserId,
    string Email,
    string DisplayName,
    List<string> Departments
);

public record MobileUserProfile(
    string UserId,
    string Email,
    string DisplayName,
    List<string> Departments
);

// ─── Shared filter ────────────────────────────────────────────────────────────

public class MobileReportFilter
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? PlantId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public DateTime EffectiveDateFrom =>
        (DateFrom ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)).Date;

    public DateTime EffectiveDateTo =>
        (DateTo ?? DateTime.UtcNow).Date;
}

public class MobileSalesFilter : MobileReportFilter
{
    public string? CustomerId { get; set; }
    public string? MaterialNumber { get; set; }
}

// ─── Dashboard ────────────────────────────────────────────────────────────────

public record MobileDashboardDto(
    // Production
    int ProductionOrdersThisMonth,
    int ActiveWorkOrders,
    decimal TotalProductionQtyThisMonth,
    decimal DefectPercentThisMonth,
    // Sales
    decimal TotalRevenueThisMonth,
    decimal CollectedRevenueThisMonth,
    decimal OutstandingRevenueThisMonth,
    int InvoicesThisMonth,
    int PendingInvoices,
    // Inventory
    decimal TotalStockValue,
    int FinishedGoodsLines,
    // Returns
    int SalesReturnsThisMonth
);

// ─── Daily Production ─────────────────────────────────────────────────────────

public record DailyProductionRow(
    DateTime ProductionDate,
    string ProductionOrderNumber,
    string? DocumentNumber,
    string MaterialNumber,
    string MaterialDescription,
    decimal ProducedQty,
    decimal GoodQty,        // First + Second + Third
    decimal DefectiveQty,   // Scrap/Rejected
    decimal WastageQty,     // Stage-level wastage
    decimal DefectPercent,
    string BatchNo,
    string Status
);

// ─── Production Summary ───────────────────────────────────────────────────────

public record ProductionSummaryKpis(
    int TotalOrders,
    decimal TotalProducedQty,
    decimal TotalGoodQty,
    decimal TotalDefectiveQty,
    decimal TotalWastageQty,
    decimal OverallDefectPercent,
    int CompletedOrders,
    int InProgressOrders,
    int PlannedOrders,
    int ReleasedOrders
);

public record ProductionSummaryRow(
    string Label,            // "2026-W18" or "2026-05"
    int OrderCount,
    decimal ProducedQty,
    decimal GoodQty,
    decimal DefectiveQty,
    decimal DefectPercent
);

public record ProductionSummaryDto(
    ProductionSummaryKpis Kpis,
    List<ProductionSummaryRow> ChartSeries
);

// ─── Defect / Wastage ─────────────────────────────────────────────────────────

public record DefectKpis(
    decimal TotalProduced,
    decimal TotalFirstQuality,
    decimal TotalSecondQuality,
    decimal TotalThirdQuality,
    decimal TotalScrap,
    decimal TotalStageWastage,
    decimal ScrapPercent
);

public record DefectReportRow(
    string ProductionOrderNumber,
    string? DocumentNumber,
    string MaterialNumber,
    string MaterialDescription,
    DateTime BatchDate,
    decimal ProducedQty,
    decimal FirstQualityQty,
    decimal SecondQualityQty,
    decimal ThirdQualityQty,
    decimal ScrapQty,
    decimal StageWastageQty,
    decimal DefectPercent,
    string BatchNo
);

public record DefectReportDto(
    DefectKpis Kpis,
    PagedResult<DefectReportRow> Rows
);

// ─── Raw Material Consumption ─────────────────────────────────────────────────

public record RawMaterialKpis(
    int TotalMaterialsConsumed,
    decimal TotalIssuedLines
);

public record RawMaterialConsumptionRow(
    string DocumentNumber,
    DateTime DocumentDate,
    string MaterialNumber,
    string MaterialDescription,
    decimal IssuedQty,
    string Uom,
    string? ForMaterialNumber,
    string? ForMaterialDescription
);

public record RawMaterialConsumptionDto(
    RawMaterialKpis Kpis,
    PagedResult<RawMaterialConsumptionRow> Rows
);

// ─── Batch / Lot Tracking ─────────────────────────────────────────────────────

public record BatchTrackingRow(
    string BatchNo,
    string ProductionOrderNumber,
    string? DocumentNumber,
    string MaterialNumber,
    string MaterialDescription,
    DateTime BatchDate,
    decimal ProducedQty,
    decimal FirstQualityQty,
    decimal SecondQualityQty,
    decimal ThirdQualityQty,
    decimal ScrapQty,
    string QualityStatus   // "Excellent" ≥90% first, "Good" ≥70%, "Mixed", "Poor"
);

// ─── Work Orders ──────────────────────────────────────────────────────────────

public record WorkOrderKpis(
    int Total,
    int Planned,
    int Released,
    int InProgress,
    int Completed
);

public record WorkOrderRow(
    int Id,
    string ProductionOrderNumber,
    string? DocumentNumber,
    string MaterialNumber,
    string MaterialDescription,
    int TargetQuantity,
    string Uom,
    DateTime PlannedStartDate,
    DateTime PlannedEndDate,
    string Status,
    string Priority,
    int StagesDone,
    int StagesTotal
);

public record WorkOrdersDto(
    WorkOrderKpis Kpis,
    PagedResult<WorkOrderRow> Rows
);

// ─── Finished Goods Inventory ─────────────────────────────────────────────────

public record FinishedGoodsKpis(
    int TotalLines,
    decimal TotalStockValue,
    int ZeroStockLines
);

public record FinishedGoodsRow(
    string MaterialNumber,
    string MaterialDescription,
    string PlantId,
    string PlantName,
    decimal Quantity,
    string Uom,
    string Grade,
    string? BatchOrLot,
    decimal StandardCostPerUom,
    decimal StockValue
);

public record FinishedGoodsDto(
    FinishedGoodsKpis Kpis,
    PagedResult<FinishedGoodsRow> Rows
);

// ─── Sales Summary ────────────────────────────────────────────────────────────

public record SalesSummaryKpis(
    decimal TotalRevenue,
    decimal CollectedRevenue,
    decimal OutstandingRevenue,
    int TotalInvoices,
    int PaidInvoices,
    int UnpaidInvoices,
    int ReturnInProcessInvoices,
    decimal AverageInvoiceValue,
    int TotalOrders,
    int TotalReturns,
    decimal TotalReturnCreditValue
);

public record SalesSummaryChartPoint(string Label, decimal Revenue, decimal Collected);

public record SalesSummaryDto(
    SalesSummaryKpis Kpis,
    List<SalesSummaryChartPoint> ChartSeries
);

// ─── Invoice List ─────────────────────────────────────────────────────────────

public record InvoiceRow(
    int Id,
    string DocumentNumber,
    DateTime DocumentDate,
    DateTime DueDate,
    string? CustomerName,
    string DcNumber,
    decimal Subtotal,
    decimal GrandTotal,
    string Status
);

// ─── Customer Wise Sales ──────────────────────────────────────────────────────

public record CustomerSalesRow(
    string? CustomerId,
    string CustomerName,
    int InvoiceCount,
    decimal TotalAmount,
    decimal CollectedAmount,
    decimal OutstandingAmount,
    DateTime? LastInvoiceDate
);

// ─── Product Wise Sales ───────────────────────────────────────────────────────

public record ProductSalesRow(
    string MaterialNumber,
    string MaterialDescription,
    decimal TotalQty,
    string? Uom,
    decimal TotalRevenue,
    int InvoiceLineCount
);

// ─── Sales Returns ────────────────────────────────────────────────────────────

public record SalesReturnKpis(
    int TotalReturns,
    decimal TotalReturnValue,
    int PendingQualityInspection,
    int CreditMemoIssued,
    decimal TotalCreditIssued
);

public record SalesReturnRow(
    int Id,
    string DocumentNumber,
    DateTime DocumentDate,
    string? CustomerName,
    string InvoiceNumber,
    decimal InvoiceTotal,
    string ReturnReason,
    bool HasQualityInspection,
    bool HasCreditMemo,
    decimal? CreditAmount
);

public record SalesReturnsDto(
    SalesReturnKpis Kpis,
    PagedResult<SalesReturnRow> Rows
);

// ─── Lookups ────────────────────────────────────────────────────────────────────

public record MobileLookupItem(string Id, string Name);

public record MobileProductLookupItem(string MaterialNumber, string Description);
