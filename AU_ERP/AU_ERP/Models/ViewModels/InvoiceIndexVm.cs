namespace AU_ERP.Models.ViewModels;

public sealed class InvoiceIndexVm
{
    public int OpenCount { get; set; }
    public decimal OpenAmount { get; set; }
    public int OverdueCount { get; set; }
    public decimal OverdueAmount { get; set; }
    public int CollectedCount { get; set; }
    public decimal CollectedAmount { get; set; }

    public int ReturnInProcessCount { get; set; }
    public decimal ReturnInProcessAmount { get; set; }

    public List<SalesInvoice> Invoices { get; set; } = new();

    /// <summary>Invoice id → return order id when a return already exists.</summary>
    public Dictionary<int, int> ReturnOrderIdByInvoiceId { get; set; } = new();

    /// <summary>Invoice id → linked sales return QI (for QI / View QI buttons).</summary>
    public Dictionary<int, InvoiceQiNavInfo> QiByInvoiceId { get; set; } = new();
}

public sealed class InvoiceQiNavInfo
{
    public int QiId { get; set; }
    public bool IsPending { get; set; }
}
