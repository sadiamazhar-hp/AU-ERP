using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
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
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewData["Title"] = "Drivers and vehicles";
        var vm = new DriversAndVehiclesIndexVm
        {
            Drivers = await _db.Drivers.AsNoTracking().OrderByDescending(d => d.CreatedAt).ThenBy(d => d.Id).ToListAsync(ct),
            Vehicles = await _db.Vehicles.AsNoTracking().OrderByDescending(v => v.CreatedAt).ThenBy(v => v.Id).ToListAsync(ct)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDriver(
        [Bind(nameof(Driver.FirstName), nameof(Driver.LastName), nameof(Driver.CNIC), nameof(Driver.LicenceNo), nameof(Driver.Mobile), nameof(Driver.IsActive))]
        Driver model,
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
        if (string.IsNullOrWhiteSpace(model.LicenceNo))
            ModelState.AddModelError(nameof(Driver.LicenceNo), "Licence number is required.");

        if (!ModelState.IsValid)
        {
            ViewBag.ActiveTab = "drivers";
            ViewBag.ShowDriverModal = true;
            var vm = await BuildIndexVmAsync(ct);
            return View("Index", vm);
        }

        model.CreatedAt = DateTime.UtcNow;
        _db.Drivers.Add(model);
        await _db.SaveChangesAsync(ct);
        TempData["DriversVehiclesMessage"] = "Driver saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVehicle(
        [Bind(nameof(Vehicle.NumberPlate), nameof(Vehicle.CarType), nameof(Vehicle.IsActive))]
        Vehicle model,
        CancellationToken ct = default)
    {
        model.NumberPlate = string.IsNullOrWhiteSpace(model.NumberPlate) ? "" : model.NumberPlate.Trim();
        model.CarType = string.IsNullOrWhiteSpace(model.CarType) ? "" : model.CarType.Trim();

        if (string.IsNullOrWhiteSpace(model.NumberPlate))
            ModelState.AddModelError(nameof(Vehicle.NumberPlate), "Number plate is required.");
        if (!VehicleCarTypes.All.Contains(model.CarType))
            ModelState.AddModelError(nameof(Vehicle.CarType), "Select a valid car type.");

        if (await _db.Vehicles.AnyAsync(v => v.NumberPlate == model.NumberPlate, ct))
            ModelState.AddModelError(nameof(Vehicle.NumberPlate), "This number plate is already registered.");

        if (!ModelState.IsValid)
        {
            ViewBag.ActiveTab = "vehicles";
            ViewBag.ShowVehicleModal = true;
            var vm = await BuildIndexVmAsync(ct);
            return View("Index", vm);
        }

        model.CreatedAt = DateTime.UtcNow;
        _db.Vehicles.Add(model);
        await _db.SaveChangesAsync(ct);
        TempData["DriversVehiclesMessage"] = "Vehicle saved.";
        return RedirectToAction(nameof(Index), new { tab = "vehicles" });
    }

    private async Task<DriversAndVehiclesIndexVm> BuildIndexVmAsync(CancellationToken ct) =>
        new DriversAndVehiclesIndexVm
        {
            Drivers = await _db.Drivers.AsNoTracking().OrderByDescending(d => d.CreatedAt).ThenBy(d => d.Id).ToListAsync(ct),
            Vehicles = await _db.Vehicles.AsNoTracking().OrderByDescending(v => v.CreatedAt).ThenBy(v => v.Id).ToListAsync(ct)
        };
}
