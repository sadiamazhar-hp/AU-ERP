using AU_ERP.Models;

namespace AU_ERP.Models.ViewModels;

public class DriversAndVehiclesIndexVm
{
    public List<Driver> Drivers { get; set; } = new();
    public List<Vehicle> Vehicles { get; set; } = new();
}
