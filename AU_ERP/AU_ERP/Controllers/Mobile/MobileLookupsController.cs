using AU_ERP.Models.Mobile;
using AU_ERP.Services.Mobile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AU_ERP.Controllers.Mobile;

[ApiController]
[Route("api/mobile/lookups")]
[Authorize(Policy = "MobileApi")]
public class MobileLookupsController : ControllerBase
{
    private readonly MobileReportingService _reports;

    public MobileLookupsController(MobileReportingService reports) => _reports = reports;

    [HttpGet("plants")]
    public async Task<ActionResult<MobileApiResponse<List<MobileLookupItem>>>> GetPlants(
        [FromQuery] string? search,
        [FromQuery] int? take,
        CancellationToken ct)
    {
        var data = await _reports.GetPlantsLookupAsync(search, take ?? 50, ct);
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
