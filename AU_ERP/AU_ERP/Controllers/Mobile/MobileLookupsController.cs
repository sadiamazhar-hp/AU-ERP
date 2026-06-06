using AU_ERP.Models;
using AU_ERP.Models.Mobile;
using AU_ERP.Services;
using AU_ERP.Services.Mobile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace AU_ERP.Controllers.Mobile;

[ApiController]
[Route("api/mobile/lookups")]
[Authorize(Policy = "MobileApi")]
public class MobileLookupsController : ControllerBase
{
    private readonly MobileReportingService _reports;
    private readonly AppDbContext _db;

    public MobileLookupsController(MobileReportingService reports, AppDbContext db)
    {
        _reports = reports;
        _db = db;
    }

    [HttpGet("plants")]
    public async Task<ActionResult<MobileApiResponse<List<MobileLookupItem>>>> GetPlants(
        [FromQuery] string? search,
        [FromQuery] int? take,
        CancellationToken ct)
    {
        var allPlantIds = await _db.PlantsSamples.AsNoTracking().Select(p => p.PlantID).ToListAsync(ct);
        var assignedPlantIds = await SalesPlantAccess.LoadAssignedPlantIdsAsync(_db, User, allPlantIds, ct);
        var assignedSet = assignedPlantIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isAdminAllPlants = UserPlantResolution.IsAdminDepartment(User) && assignedPlantIds.Count == 0;

        var data = await _reports.GetPlantsLookupAsync(search, take ?? 200, ct);
        if (!isAdminAllPlants)
            data = data.Where(x => assignedSet.Contains(x.Id)).ToList();

        return Ok(MobileApiResponse<List<MobileLookupItem>>.Ok(data));
    }

    [HttpGet("customers")]
    public async Task<ActionResult<MobileApiResponse<List<MobileLookupItem>>>> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] int? take,
        CancellationToken ct)
    {
        var data = await _reports.GetCustomersLookupAsync(search, take ?? 50, ct);
        return Ok(MobileApiResponse<List<MobileLookupItem>>.Ok(data));
    }

    [HttpGet("products")]
    public async Task<ActionResult<MobileApiResponse<List<MobileProductLookupItem>>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] int? take,
        CancellationToken ct)
    {
        var data = await _reports.GetProductsLookupAsync(search, take ?? 50, ct);
        return Ok(MobileApiResponse<List<MobileProductLookupItem>>.Ok(data));
    }
}
