using AU_ERP.Models;

namespace AU_ERP.Models.ViewModels;

public class StockMovementIndexVm
{
    public IReadOnlyList<StockMovement> Rows { get; set; } = Array.Empty<StockMovement>();

    /// <summary>Contains match on material number or description.</summary>
    public string? MaterialFilter { get; set; }

    public string? FromPlantFilter { get; set; }
    public string? ToPlantFilter { get; set; }

    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    /// <summary>Contains match on movement number.</summary>
    public string? SearchNo { get; set; }

    /// <summary>Default from-plant for the new transfer form (first assigned plant).</summary>
    public string DefaultFromPlantId { get; set; } = "";

    public List<StockMovementPlantOptionVm> FromPlantOptions { get; set; } = new();
    public List<StockMovementPlantOptionVm> AllPlantOptions { get; set; } = new();
}

public class StockMovementPlantOptionVm
{
    public string PlantId { get; set; } = "";
    public string PlantName { get; set; } = "";
}
