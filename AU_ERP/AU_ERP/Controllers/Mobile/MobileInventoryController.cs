using AU_ERP.Models.Mobile;
using AU_ERP.Services.Mobile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AU_ERP.Controllers.Mobile;

[ApiController]
[Route("api/mobile/reports/inventory")]
[Authorize(Policy = "MobileApi")]
public class MobileInventoryController : ControllerBase
{
    private readonly MobileReportingService _reports;

    public MobileInventoryController(MobileReportingService reports) => _reports = reports;

    /// <summary>
    /// Finished goods stock lines (FERT materials only) with grade, batch, and stock value.
    /// Query params: plantId, page, pageSize
    /// </summary>
    [HttpGet("finished-goods")]
    public async Task<ActionResult<MobileApiResponse<FinishedGoodsDto>>> FinishedGoods(
        [FromQuery] MobileReportFilter filter, CancellationToken ct)
    {
        var data = await _reports.GetFinishedGoodsAsync(filter, ct, User);
        return Ok(MobileApiResponse<FinishedGoodsDto>.Ok(data));
    }
}
