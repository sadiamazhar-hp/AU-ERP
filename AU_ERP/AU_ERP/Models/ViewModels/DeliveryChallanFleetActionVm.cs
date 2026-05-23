namespace AU_ERP.Models.ViewModels;

/// <summary>Delivery challan fleet / delivery-completed state for list actions.</summary>
public sealed class DeliveryChallanFleetActionVm
{
    public int DeliveryChallanId { get; init; }
    public string DeliveryChallanNumber { get; init; } = "";
    public bool IsDeliveryCompleted { get; init; }
    public bool CanMarkDeliveryCompleted { get; init; }
}
