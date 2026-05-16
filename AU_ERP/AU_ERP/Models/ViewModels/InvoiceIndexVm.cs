namespace AU_ERP.Models.ViewModels;

public sealed class InvoiceIndexVm
{
    /// <summary>Active filter inputs (posted via GET).</summary>
    public string? SearchQuery { get; set; }
    /// <summary>Matches <see cref="SalesInvoice"/> status presets: empty = all.</summary>
    public string? StatusFilter { get; set; }

    /// <summary>Inclusive document-date lower bound.</summary>
    public DateTime? DocumentDateFrom { get; set; }

    /// <summary>Inclusive document-date upper bound.</summary>
    public DateTime? DocumentDateTo { get; set; }

    public int OpenCount { get; set; }
    public decimal OpenAmount { get; set; }
    public int OverdueCount { get; set; }
    public decimal OverdueAmount { get; set; }
    public int CollectedCount { get; set; }
    public decimal CollectedAmount { get; set; }

    public int ReturnInProcessCount { get; set; }
    public decimal ReturnInProcessAmount { get; set; }

    public List<SalesInvoice> Invoices { get; set; } = new();

    /// <summary>Invoice id → linked return orders (newest document date first).</summary>
    public Dictionary<int, List<InvoiceReturnOrderSummaryVm>> ReturnOrdersByInvoiceId { get; set; } = new();

    /// <summary>Invoice id → linked sales return QI (for QI / View QI buttons).</summary>
    public Dictionary<int, InvoiceQiNavInfo> QiByInvoiceId { get; set; } = new();

    /// <summary>Invoice id → return order id for credit-memo navigation (newest return with a credit memo).</summary>
    public Dictionary<int, int> CreditMemoReturnOrderIdByInvoiceId { get; set; } = new();
}

public sealed class InvoiceReturnOrdersListVm
{
    public int InvoiceId { get; set; }
    public string? InvoiceDocumentNumber { get; set; }
    public List<InvoiceReturnOrderSummaryVm> Orders { get; set; } = new();
}

/// <summary>Minimal return order row for invoice modals.</summary>
public sealed class InvoiceReturnOrderSummaryVm
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateTime DocumentDate { get; set; }
}

public sealed class InvoiceQiNavInfo
{
    public int QiId { get; set; }
    public bool IsPending { get; set; }
}
