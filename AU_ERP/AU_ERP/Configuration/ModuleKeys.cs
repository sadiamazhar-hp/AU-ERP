namespace AU_ERP.Configuration;

public static class ModuleKeys
{
    public const string SaleQuotation = "SaleQuotation";
    public const string SaleOrder = "SaleOrder";
    public const string SalesGoodsIssue = "SalesGoodsIssue";
    /// <summary>Unified goods issue document numbers (reservation + sales dispatch).</summary>
    public const string StockGoodsIssue = "StockGoodsIssue";
    public const string DeliveryChallan = "DeliveryChallan";
    public const string Invoice = "Invoice";
    public const string Payment = "Payment";
    public const string ReturnOrder = "ReturnOrder";
    public const string CreditMemo = "CreditMemo";
    public const string ProductionOrder = "ProductionOrder";
    public const string ReservationIssue = "ReservationIssue";
    public const string QualityInspection = "QualityInspection";
    public const string Batch = "Batch";

    public static readonly IReadOnlyList<(string Key, string Display)> All = new[]
    {
        (SaleQuotation, "Sales Quotation"),
        (SaleOrder, "Sales Order"),
        (SalesGoodsIssue, "Sales Goods Issue"),
        (StockGoodsIssue, "Stock Good Issue"),
        (DeliveryChallan, "Delivery Challan"),
        (Invoice, "Invoice"),
        (Payment, "Payment"),
        (ReturnOrder, "Return Order"),
        (CreditMemo, "Credit Memo"),
        (ProductionOrder, "Production Order"),
        (ReservationIssue, "Reservation Issue"),
        (QualityInspection, "Quality Inspection"),
        (Batch, "Batch"),
    };
}
