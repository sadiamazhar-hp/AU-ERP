using AU_ERP.Models;

namespace AU_ERP.Models.ViewModels;

public class DriversAndVehiclesIndexVm
{
    public string ActiveTab { get; set; } = "drivers";

    public string? DriverSearch { get; set; }
    /// <summary>Empty = all, Active, Inactive.</summary>
    public string? DriverStatusFilter { get; set; }
    /// <summary>Empty = all, Available, Busy.</summary>
    public string? DriverFleetFilter { get; set; }

    public string? VehicleSearch { get; set; }
    public string? VehicleStatusFilter { get; set; }
    public string? VehicleFleetFilter { get; set; }
    public string? VehicleCarTypeFilter { get; set; }

    public int DriverMatchCount { get; set; }
    public int VehicleMatchCount { get; set; }

    public List<Driver> Drivers { get; set; } = new();
    public List<Vehicle> Vehicles { get; set; } = new();

    /// <summary>Driver ids on an unfinished delivery assignment (invoice open/return-in-progress or no invoice yet).</summary>
    public HashSet<int> DriverIdsBusyOnOpenDeliveryInvoice { get; set; } = new();

    public HashSet<int> VehicleIdsBusyOnOpenDeliveryInvoice { get; set; } = new();

    /// <summary>Busy driver → ref. SO / order label for active delivery.</summary>
    public Dictionary<int, string> BusyDriverDeliveryRefById { get; set; } = new();

    public Dictionary<int, string> BusyVehicleDeliveryRefById { get; set; } = new();
}
