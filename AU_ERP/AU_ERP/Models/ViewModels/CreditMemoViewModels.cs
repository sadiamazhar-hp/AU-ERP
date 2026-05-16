namespace AU_ERP.Models.ViewModels;

/// <summary>List screen for sales return credit memos (Billing).</summary>
public sealed class CreditMemoIndexVm
{
    public string? SearchQuery { get; set; }

    public DateTime? DocumentDateFrom { get; set; }

    public DateTime? DocumentDateTo { get; set; }

    /// <summary>When set, the UI opens the credit memo modal for this return order.</summary>
    public int? OpenReturnOrderId { get; set; }

    public List<CreditMemoIndexRowVm> Items { get; set; } = new();
}

public sealed class CreditMemoIndexRowVm
{
    /// <summary>Sales-return credit memo document id.</summary>
    public int Id { get; set; }

    public int SalesReturnOrderId { get; set; }

    public string DocumentNumber { get; set; } = "";

    public DateTime DocumentDate { get; set; }

    public string ReturnOrderDocumentNumber { get; set; } = "";

    public string InvoiceDocumentNumber { get; set; } = "";

    public string? DealerDisplayName { get; set; }

    public decimal GrandTotalCredit { get; set; }
}
