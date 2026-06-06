using AU_ERP.Models.Mobile;
using AU_ERP.Services.Mobile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AU_ERP.Controllers.Mobile;

[ApiController]
[Route("api/mobile/dashboard")]
[Authorize(Policy = "MobileApi")]
public class MobileDashboardController : ControllerBase
{
    private readonly MobileReportingService _reports;

    public MobileDashboardController(MobileReportingService reports) => _reports = reports;

    /// <summary>
    /// Returns period-scoped KPIs across production, sales, and inventory for the home dashboard.
    /// Defaults to the last 30 days when dateFrom/dateTo are omitted.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<MobileApiResponse<MobileDashboardDto>>> Get(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct)
    {
        var data = await _reports.GetDashboardAsync(dateFrom, dateTo, ct);
        return Ok(MobileApiResponse<MobileDashboardDto>.Ok(data));
    }
}
