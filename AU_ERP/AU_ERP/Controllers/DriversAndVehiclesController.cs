using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "AdminDepartment")]
public class DriversAndVehiclesController : Controller
{
    private readonly AppDbContext _db;

    public DriversAndVehiclesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? tab,
        string? dq,
        string? dStatus,
        string? dFleet,
        string? vq,
        string? vStatus,
        string? vFleet,
        string? vCarType,
        CancellationToken ct = default)
    {
        ViewData["Title"] = "Drivers and vehicles";
        var activeTab = string.Equals(tab, "vehicles", StringComparison.OrdinalIgnoreCase) ? "vehicles" : "drivers";
        ViewBag.ActiveTab = activeTab;

        var vm = await BuildIndexVmAsync(
            activeTab,
            dq, dStatus, dFleet,
            vq, vStatus, vFleet, vCarType,
            ct).ConfigureAwait(false);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDriver(
        [Bind(nameof(Driver.FirstName), nameof(Driver.LastName), nameof(Driver.CNIC), nameof(Driver.LicenceNo), nameof(Driver.Mobile), nameof(Driver.IsActive))]
        Driver model,
        string? dq,
        string? dStatus,
        string? dFleet,
        CancellationToken ct = default)
    {
        model.FirstName = string.IsNullOrWhiteSpace(model.FirstName) ? null : model.FirstName.Trim();
        model.LastName = string.IsNullOrWhiteSpace(model.LastName) ? null : model.LastName.Trim();
        model.CNIC = string.IsNullOrWhiteSpace(model.CNIC) ? null : model.CNIC.Trim();
        model.LicenceNo = string.IsNullOrWhiteSpace(model.LicenceNo) ? null : model.LicenceNo.Trim();
        model.Mobile = string.IsNullOrWhiteSpace(model.Mobile) ? null : model.Mobile.Trim();

        if (string.IsNullOrWhiteSpace(model.FirstName))
            ModelState.AddModelError(nameof(Driver.FirstName), "First name is required.");
        if (string.IsNullOrWhiteSpace(model.LastName))
            ModelState.AddModelError(nameof(Driver.LastName), "Last name is required.");
        if (string.IsNullOrWhiteSpace(model.CNIC))
            ModelState.AddModelError(nameof(Driver.CNIC), "CNIC is required.");
        else if (!FleetDriverInputNormalizer.TryNormalizePakCnic(model.CNIC, out var cnicOk, out var cnicErr))
            ModelState.AddModelError(nameof(Driver.CNIC), cnicErr);
        else
            model.CNIC = cnicOk;

        if (string.IsNullOrWhiteSpace(model.LicenceNo))
            ModelState.AddModelError(nameof(Driver.LicenceNo), "Licence number is required.");
        else if (!FleetDriverInputNormalizer.IsValidDriverLicenceFormat(model.LicenceNo, out var licErr))
            ModelState.AddModelError(nameof(Driver.LicenceNo), licErr);

        if (string.IsNullOrWhiteSpace(model.Mobile))
            ModelState.AddModelError(nameof(Driver.Mobile), "Mobile number is required.");
        else if (!FleetDriverInputNormalizer.TryNormalizePakMobile(model.Mobile, out var mobOk, out var mobErr))
            ModelState.AddModelError(nameof(Driver.Mobile), mobErr);
        else
            model.Mobile = mobOk;

        if (!ModelState.IsValid)
        {
            ViewBag.ActiveTab = "drivers";
            ViewBag.ShowDriverModal = true;
            var vm = await BuildIndexVmAsync("drivers", dq, dStatus, dFleet, null, null, null, null, ct).ConfigureAwait(false);
            return View("Index", vm);
        }

        model.CreatedAt = DateTime.UtcNow;
        _db.Drivers.Add(model);
        await _db.SaveChangesAsync(ct);
        TempData["DriversVehiclesMessage"] = "Driver saved.";
        return RedirectToAction(nameof(Index), new { tab = "drivers", dq, dStatus, dFleet });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVehicle(
        [Bind(nameof(Vehicle.NumberPlate), nameof(Vehicle.CarType), nameof(Vehicle.IsActive))]
        Vehicle model,
        string? vq,
        string? vStatus,
        string? vFleet,
        string? vCarType,
        CancellationToken ct = default)
    {
        model.NumberPlate = string.IsNullOrWhiteSpace(model.NumberPlate) ? "" : model.NumberPlate.Trim();
        model.CarType = string.IsNullOrWhiteSpace(model.CarType) ? "" : model.CarType.Trim();

        if (string.IsNullOrWhiteSpace(model.NumberPlate))
            ModelState.AddModelError(nameof(Vehicle.NumberPlate), "Number plate is required.");
        if (!VehicleCarTypes.All.Contains(model.CarType))
            ModelState.AddModelError(nameof(Vehicle.CarType), "Select a valid car type.");

        if (!string.IsNullOrWhiteSpace(model.NumberPlate))
        {
            if (!FleetDriverInputNormalizer.IsValidVehicleNumberPlate(model.NumberPlate, out var plateFmtErr))
                ModelState.AddModelError(nameof(Vehicle.NumberPlate), plateFmtErr);
            else if (await _db.Vehicles.AnyAsync(v => v.NumberPlate == model.NumberPlate, ct).ConfigureAwait(false))
                ModelState.AddModelError(nameof(Vehicle.NumberPlate), "This number plate is already registered.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ActiveTab = "vehicles";
            ViewBag.ShowVehicleModal = true;
            var vm = await BuildIndexVmAsync("vehicles", null, null, null, vq, vStatus, vFleet, vCarType, ct).ConfigureAwait(false);
            return View("Index", vm);
        }

        model.CreatedAt = DateTime.UtcNow;
        _db.Vehicles.Add(model);
        await _db.SaveChangesAsync(ct);
        TempData["DriversVehiclesMessage"] = "Vehicle saved.";
        return RedirectToAction(nameof(Index), new { tab = "vehicles", vq, vStatus, vFleet, vCarType });
    }

    private async Task<DriversAndVehiclesIndexVm> BuildIndexVmAsync(
        string activeTab,
        string? dq,
        string? dStatus,
        string? dFleet,
        string? vq,
        string? vStatus,
        string? vFleet,
        string? vCarType,
        CancellationToken ct)
    {
        var driversBusy = await DeliveryFleetAvailability.GetDriverIdsBusyOnOpenInvoiceAsync(_db, ct)
            .ConfigureAwait(false);
        var vehiclesBusy = await DeliveryFleetAvailability.GetVehicleIdsBusyOnOpenInvoiceAsync(_db, ct)
            .ConfigureAwait(false);
        var drvRef = await DeliveryFleetAvailability.GetDriverBusyOrderLabelsAsync(_db, ct).ConfigureAwait(false);
        var vehRef = await DeliveryFleetAvailability.GetVehicleBusyOrderLabelsAsync(_db, ct).ConfigureAwait(false);

        var allDrivers = await _db.Drivers.AsNoTracking()
            .OrderByDescending(d => d.CreatedAt).ThenBy(d => d.Id)
            .ToListAsync(ct).ConfigureAwait(false);
        var allVehicles = await _db.Vehicles.AsNoTracking()
            .OrderByDescending(v => v.CreatedAt).ThenBy(v => v.Id)
            .ToListAsync(ct).ConfigureAwait(false);

        var driverSearch = (dq ?? "").Trim();
        var driverStatus = NormalizeStatusFilter(dStatus);
        var driverFleet = NormalizeFleetFilter(dFleet);
        var vehicleSearch = (vq ?? "").Trim();
        var vehicleStatus = NormalizeStatusFilter(vStatus);
        var vehicleFleet = NormalizeFleetFilter(vFleet);
        var vehicleCarType = (vCarType ?? "").Trim();

        var drivers = FilterDrivers(allDrivers, driversBusy, driverSearch, driverStatus, driverFleet);
        var vehicles = FilterVehicles(allVehicles, vehiclesBusy, vehicleSearch, vehicleStatus, vehicleFleet, vehicleCarType);

        return new DriversAndVehiclesIndexVm
        {
            ActiveTab = activeTab,
            DriverSearch = string.IsNullOrEmpty(driverSearch) ? null : driverSearch,
            DriverStatusFilter = driverStatus,
            DriverFleetFilter = driverFleet,
            VehicleSearch = string.IsNullOrEmpty(vehicleSearch) ? null : vehicleSearch,
            VehicleStatusFilter = vehicleStatus,
            VehicleFleetFilter = vehicleFleet,
            VehicleCarTypeFilter = string.IsNullOrEmpty(vehicleCarType) ? null : vehicleCarType,
            DriverMatchCount = drivers.Count,
            VehicleMatchCount = vehicles.Count,
            DriverIdsBusyOnOpenDeliveryInvoice = driversBusy,
            VehicleIdsBusyOnOpenDeliveryInvoice = vehiclesBusy,
            BusyDriverDeliveryRefById = drvRef,
            BusyVehicleDeliveryRefById = vehRef,
            Drivers = drivers,
            Vehicles = vehicles
        };
    }

    private static string? NormalizeStatusFilter(string? value)
    {
        if (string.Equals(value, "Active", StringComparison.OrdinalIgnoreCase)) return "Active";
        if (string.Equals(value, "Inactive", StringComparison.OrdinalIgnoreCase)) return "Inactive";
        return null;
    }

    private static string? NormalizeFleetFilter(string? value)
    {
        if (string.Equals(value, "Available", StringComparison.OrdinalIgnoreCase)) return "Available";
        if (string.Equals(value, "Busy", StringComparison.OrdinalIgnoreCase)) return "Busy";
        return null;
    }

    private static List<Driver> FilterDrivers(
        List<Driver> source,
        HashSet<int> busyIds,
        string search,
        string? statusFilter,
        string? fleetFilter)
    {
        IEnumerable<Driver> q = source;
        if (!string.IsNullOrEmpty(search))
        {
            q = q.Where(d =>
            {
                var name = $"{d.FirstName} {d.LastName}".Trim();
                return name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (d.CNIC ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (d.LicenceNo ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (d.Mobile ?? "").Contains(search, StringComparison.OrdinalIgnoreCase);
            });
        }

        if (statusFilter == "Active")
            q = q.Where(d => d.IsActive);
        else if (statusFilter == "Inactive")
            q = q.Where(d => !d.IsActive);

        if (fleetFilter == "Available")
            q = q.Where(d => d.IsActive && !busyIds.Contains(d.Id));
        else if (fleetFilter == "Busy")
            q = q.Where(d => d.IsActive && busyIds.Contains(d.Id));

        return q.ToList();
    }

    private static List<Vehicle> FilterVehicles(
        List<Vehicle> source,
        HashSet<int> busyIds,
        string search,
        string? statusFilter,
        string? fleetFilter,
        string carTypeFilter)
    {
        IEnumerable<Vehicle> q = source;
        if (!string.IsNullOrEmpty(search))
        {
            q = q.Where(v =>
                v.NumberPlate.Contains(search, StringComparison.OrdinalIgnoreCase)
                || v.CarType.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(carTypeFilter))
            q = q.Where(v => string.Equals(v.CarType, carTypeFilter, StringComparison.OrdinalIgnoreCase));

        if (statusFilter == "Active")
            q = q.Where(v => v.IsActive);
        else if (statusFilter == "Inactive")
            q = q.Where(v => !v.IsActive);

        if (fleetFilter == "Available")
            q = q.Where(v => v.IsActive && !busyIds.Contains(v.Id));
        else if (fleetFilter == "Busy")
            q = q.Where(v => v.IsActive && busyIds.Contains(v.Id));

        return q.ToList();
    }
}
