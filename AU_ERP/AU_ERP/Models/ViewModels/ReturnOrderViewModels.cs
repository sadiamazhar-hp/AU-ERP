using AU_ERP.Models;

namespace AU_ERP.Models.ViewModels;

public sealed class ReturnOrderIndexVm
{
    public string? FilterQuery { get; set; }
    public List<ReturnOrderIndexRowVm> Orders { get; set; } = new();
}

public sealed class ReturnOrderIndexRowVm
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateTime DocumentDate { get; set; }
    public string InvoiceDocumentNumber { get; set; } = "";
    public string? DealerDisplayName { get; set; }
    public string ReturnReasonSnippet { get; set; } = "";
    public bool HasCreditMemo { get; set; }
    public string? CreditMemoDocumentNumber { get; set; }
}

/// <summary>Payload for credit memo read-only modal on return order list.</summary>
public sealed class ReturnOrderCreditMemoModalVm
{
    public int ReturnOrderId { get; set; }
    public SalesReturnCreditMemo CreditMemo { get; set; } = null!;
}

public sealed class ReturnOrderCreateVm
{
    public int InvoiceId { get; set; }
    public DateTime DocumentDate { get; set; } = DateTime.Today;
    public string ReturnReason { get; set; } = "";

    public string? DealerBusinessPartnerId { get; set; }
    public string? DealerDisplayName { get; set; }
    public string? SalesOrderNumber { get; set; }
    public DateTime DeliveryChallanDocumentDate { get; set; }
    public DateTime? SalesOrderRequestedDeliveryDate { get; set; }
    public string InvoiceDocumentNumber { get; set; } = "";
    public decimal InvoiceGrandTotal { get; set; }
    public decimal ItemsDeliveredQuantityTotal { get; set; }

    public List<ReturnOrderCreateLineVm> Lines { get; set; } = new();
}

public sealed class ReturnOrderCreateLineVm
{
    public int SalesInvoiceLineId { get; set; }
    public int LineNo { get; set; }
    public string MaterialNumber { get; set; } = "";
    public string? MaterialDescription { get; set; }
    public string? UomCode { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal QuantityInvoiced { get; set; }
    public decimal QuantityReturned { get; set; }
}

public sealed class ReturnOrderDetailsVm
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateTime DocumentDate { get; set; }
    public string ReturnReason { get; set; } = "";
    public string InvoiceDocumentNumber { get; set; } = "";
    public int SalesInvoiceId { get; set; }
    public string? DealerBusinessPartnerId { get; set; }
    public string? DealerDisplayName { get; set; }
    public string? SalesOrderNumber { get; set; }
    public DateTime DeliveryChallanDocumentDate { get; set; }
    public DateTime? SalesOrderRequestedDeliveryDate { get; set; }
    public decimal InvoiceGrandTotal { get; set; }
    public decimal ItemsDeliveredQuantityTotal { get; set; }
    public List<ReturnOrderCreateLineVm> Lines { get; set; } = new();

    /// <summary>Populated when a linked credit memo exists (created on return save).</summary>
    public int? CreditMemoId { get; set; }

    public string? CreditMemoDocumentNumber { get; set; }
    public decimal? CreditMemoGrandTotal { get; set; }
}
