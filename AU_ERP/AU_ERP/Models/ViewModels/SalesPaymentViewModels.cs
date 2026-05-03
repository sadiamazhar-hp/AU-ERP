namespace AU_ERP.Models.ViewModels;

public sealed class SalesPaymentIndexVm
{
    public List<SalesPaymentListRowVm> Payments { get; set; } = new();
}

public sealed class SalesPaymentListRowVm
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateTime DocumentDate { get; set; }
    public string InvoiceDocumentNumber { get; set; } = "";
    public string? DealerDisplayName { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "";
}

public sealed class SalesPaymentEditVm
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateTime DocumentDate { get; set; }
    public string InvoiceDocumentNumber { get; set; } = "";
    public string? DealerDisplayName { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string? ChequeNumber { get; set; }
}
