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
    /// Returns current-month KPIs across production, sales, and inventory for the home dashboard.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<MobileApiResponse<MobileDashboardDto>>> Get(CancellationToken ct)
    {
        var data = await _reports.GetDashboardAsync(ct);
        return Ok(MobileApiResponse<MobileDashboardDto>.Ok(data));
    }
}
