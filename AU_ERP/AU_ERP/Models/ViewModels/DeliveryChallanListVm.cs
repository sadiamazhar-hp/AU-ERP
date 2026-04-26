namespace AU_ERP.Models.ViewModels;

public class DeliveryChallanListVm
{
    public List<AU_ERP.Models.DeliveryChallan> Items { get; set; } = new();

    public string? Q { get; set; }

    public string? PlantId { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }
}
