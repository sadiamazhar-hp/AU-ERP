using AU_ERP.Models.Mobile;
using AU_ERP.Services.Mobile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AU_ERP.Controllers.Mobile;

[ApiController]
[Route("api/mobile/reports/production")]
[Authorize(Policy = "MobileApi")]
public class MobileProductionController : ControllerBase
{
    private readonly MobileReportingService _reports;

    public MobileProductionController(MobileReportingService reports) => _reports = reports;

    /// <summary>
    /// Daily production output: one row per goods-receipt batch.
    /// Query params: dateFrom, dateTo, plantId, page, pageSize
    /// </summary>
    [HttpGet("daily")]
    public async Task<ActionResult<MobileApiResponse<PagedResult<DailyProductionRow>>>> Daily(
        [FromQuery] MobileReportFilter filter, CancellationToken ct)
    {
        var data = await _reports.GetDailyProductionAsync(filter, ct);
        return Ok(MobileApiResponse<PagedResult<DailyProductionRow>>.Ok(data));
    }

    /// <summary>
    /// Production summary KPIs + weekly chart series for a date range.
    /// Query params: dateFrom, dateTo, plantId
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<MobileApiResponse<ProductionSummaryDto>>> Summary(
        [FromQuery] MobileReportFilter filter, CancellationToken ct)
    {
        var data = await _reports.GetProductionSummaryAsync(filter, ct);
        return Ok(MobileApiResponse<ProductionSummaryDto>.Ok(data));
    }

    /// <summary>
    /// Defect and wastage breakdown by batch.
    /// Query params: dateFrom, dateTo, plantId, page, pageSize
    /// </summary>
    [HttpGet("defects")]
    public async Task<ActionResult<MobileApiResponse<DefectReportDto>>> Defects(
        [FromQuery] MobileReportFilter filter, CancellationToken ct)
    {
        var data = await _reports.GetDefectsAsync(filter, ct);
        return Ok(MobileApiResponse<DefectReportDto>.Ok(data));
    }

    /// <summary>
    /// Raw material consumption from completed goods-issue documents.
    /// Query params: dateFrom, dateTo, page, pageSize
    /// </summary>
    [HttpGet("raw-materials")]
    public async Task<ActionResult<MobileApiResponse<RawMaterialConsumptionDto>>> RawMaterials(
        [FromQuery] MobileReportFilter filter, CancellationToken ct)
    {
        var data = await _reports.GetRawMaterialConsumptionAsync(filter, ct);
        return Ok(MobileApiResponse<RawMaterialConsumptionDto>.Ok(data));
    }

    /// <summary>
    /// Batch/lot traceability: all production batches with quality grade breakdown.
    /// Query params: dateFrom, dateTo, plantId, page, pageSize
    /// </summary>
    [HttpGet("batches")]
    public async Task<ActionResult<MobileApiResponse<PagedResult<BatchTrackingRow>>>> Batches(
        [FromQuery] MobileReportFilter filter, CancellationToken ct)
    {
        var data = await _reports.GetBatchTrackingAsync(filter, ct);
        return Ok(MobileApiResponse<PagedResult<BatchTrackingRow>>.Ok(data));
    }

    /// <summary>
    /// Work order status list with KPI counts (Planned / Released / InProgress / Completed).
    /// Query params: dateFrom, dateTo, plantId, page, pageSize
    /// </summary>
    [HttpGet("work-orders")]
    public async Task<ActionResult<MobileApiResponse<WorkOrdersDto>>> WorkOrders(
        [FromQuery] MobileReportFilter filter, CancellationToken ct)
    {
        var data = await _reports.GetWorkOrdersAsync(filter, ct);
        return Ok(MobileApiResponse<WorkOrdersDto>.Ok(data));
    }
}
