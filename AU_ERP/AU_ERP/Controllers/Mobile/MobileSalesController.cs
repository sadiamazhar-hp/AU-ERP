using AU_ERP.Models.Mobile;
using AU_ERP.Services;
using AU_ERP.Services.Mobile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AU_ERP.Controllers.Mobile;

[ApiController]
[Route("api/mobile/reports/sales")]
[Authorize(Policy = "MobileApi")]
public class MobileSalesController : ControllerBase
{
    private readonly MobileReportingService _reports;

    public MobileSalesController(MobileReportingService reports) => _reports = reports;

    /// <summary>
    /// Sales summary KPIs (revenue, collected, outstanding, orders) + monthly chart series.
    /// Query params: dateFrom, dateTo, customerId
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<MobileApiResponse<SalesSummaryDto>>> Summary(
        [FromQuery] MobileSalesFilter filter, CancellationToken ct)
    {
        var data = await _reports.GetSalesSummaryAsync(filter, ct);
        return Ok(MobileApiResponse<SalesSummaryDto>.Ok(data));
    }

    /// <summary>
    /// Invoice list with status (Open / Collected / ReturnInProcess / Returned).
    /// Query params: dateFrom, dateTo, customerId, page, pageSize
    /// </summary>
    [HttpGet("invoices")]
    public async Task<ActionResult<MobileApiResponse<PagedResult<InvoiceRow>>>> Invoices(
        [FromQuery] MobileSalesFilter filter, CancellationToken ct)
    {
        if (!User.HasClaim(AuClaimTypes.Department, "Finance"))
            return StatusCode(StatusCodes.Status403Forbidden, MobileApiResponse<PagedResult<InvoiceRow>>.Fail("Finance department access required."));

        var data = await _reports.GetInvoicesAsync(filter, ct);
        return Ok(MobileApiResponse<PagedResult<InvoiceRow>>.Ok(data));
    }

    /// <summary>
    /// Customer-wise sales: total revenue, collected, outstanding, invoice count.
    /// Query params: dateFrom, dateTo, page, pageSize
    /// </summary>
    [HttpGet("customers")]
    public async Task<ActionResult<MobileApiResponse<PagedResult<CustomerSalesRow>>>> Customers(
        [FromQuery] MobileSalesFilter filter, CancellationToken ct)
    {
        var data = await _reports.GetCustomerSalesAsync(filter, ct);
        return Ok(MobileApiResponse<PagedResult<CustomerSalesRow>>.Ok(data));
    }

    /// <summary>
    /// Product-wise sales: quantity sold, revenue, invoice count per material.
    /// Query params: dateFrom, dateTo, materialNumber, customerId, page, pageSize
    /// </summary>
    [HttpGet("products")]
    public async Task<ActionResult<MobileApiResponse<PagedResult<ProductSalesRow>>>> Products(
        [FromQuery] MobileSalesFilter filter, CancellationToken ct)
    {
        var data = await _reports.GetProductSalesAsync(filter, ct);
        return Ok(MobileApiResponse<PagedResult<ProductSalesRow>>.Ok(data));
    }

    /// <summary>
    /// Sales return list with credit memo and quality inspection status.
    /// Query params: dateFrom, dateTo, customerId, page, pageSize
    /// </summary>
    [HttpGet("returns")]
    public async Task<ActionResult<MobileApiResponse<SalesReturnsDto>>> Returns(
        [FromQuery] MobileSalesFilter filter, CancellationToken ct)
    {
        var data = await _reports.GetSalesReturnsAsync(filter, ct);
        return Ok(MobileApiResponse<SalesReturnsDto>.Ok(data));
    }
}
