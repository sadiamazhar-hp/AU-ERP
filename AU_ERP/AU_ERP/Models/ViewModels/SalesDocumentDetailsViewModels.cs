using AU_ERP.Models;

namespace AU_ERP.Models.ViewModels;

/// <summary>Read-only pricing breakdown for sales order / quotation view modals (matches create modal totals logic).</summary>
public sealed class SalesDocumentDetailsModalVm
{
    public IReadOnlyList<SalesLineChargeColVm> ItemChargeColumns { get; init; } = Array.Empty<SalesLineChargeColVm>();
    public IReadOnlyList<SalesLineDisplayVm> Lines { get; init; } = Array.Empty<SalesLineDisplayVm>();
    /// <summary>Sum of line net (PKR) before document-level charges — same as “Subtotal” in create form.</summary>
    public decimal SubtotalLineTotals { get; init; }
    public IReadOnlyList<SalesDocChargeStepVm> DocumentChargeSteps { get; init; } = Array.Empty<SalesDocChargeStepVm>();
    public decimal GrandTotal { get; init; }
}

public sealed class SalesLineChargeColVm
{
    public int ChargeId { get; init; }
    public string Symbol { get; init; } = "";
    public bool IsPercentage { get; init; }
}

public sealed class SalesLineDisplayVm
{
    public string MaterialNumber { get; init; } = "";
    public string? MaterialDescription { get; init; }
    public string? UomCode { get; init; }
    public decimal Qty { get; init; }
    public decimal UnitPrice { get; init; }
    /// <summary>Qty × unit (after line discount) before item column charges — matches stored <c>SubtotalAfterDiscount</c>.</summary>
    public decimal SubtotalBeforeItemCharges { get; init; }
    /// <summary>User-entered values keyed by charge id (for dynamic columns).</summary>
    public IReadOnlyDictionary<int, decimal> ItemChargeValues { get; init; } = new Dictionary<int, decimal>();
    public decimal LineNet { get; init; }
}

public sealed class SalesDocChargeStepVm
{
    public int ChargeId { get; init; }
    public string Symbol { get; init; } = "";
    public string? Description { get; init; }
    public bool IsPercentage { get; init; }
    /// <summary>Effective % or flat amount from posted JSON / defaults.</summary>
    public decimal InputValue { get; init; }
    public decimal Delta { get; init; }
    public decimal RunningAfter { get; init; }
}

public sealed class SalesOrderDetailsFullVm
{
    public SalesOrder Order { get; init; } = null!;
    public SalesDocumentDetailsModalVm Pricing { get; init; } = null!;
}

public sealed class SalesQuotationDetailsFullVm
{
    public SalesQuotation Quotation { get; init; } = null!;
    public SalesDocumentDetailsModalVm Pricing { get; init; } = null!;
}
