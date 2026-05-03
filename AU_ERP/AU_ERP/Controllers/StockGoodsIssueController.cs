using System.IO;
using AU_ERP.Models;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace AU_ERP.Controllers;

[Authorize(Policy = "InventoryGoodsIssue")]
public class StockGoodsIssueController : Controller
{
    private readonly AppDbContext _db;
    private readonly GoodsIssueService _reservationGi;
    private readonly SalesGoodsIssueService _salesGi;
    private readonly CompanyInfoService _companyInfo;

    public StockGoodsIssueController(
        AppDbContext db,
        GoodsIssueService reservationGi,
        SalesGoodsIssueService salesGi,
        CompanyInfoService companyInfo)
    {
        _db = db;
        _reservationGi = reservationGi;
        _salesGi = salesGi;
        _companyInfo = companyInfo;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? productionOrderId,
        int? salesOrderId,
        string? q,
        string? dispatch,
        string? source,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken ct = default)
    {
        var reservation = await _db.GoodsIssueDocuments.AsNoTracking()
            .Include(d => d.ProductionOrder!)
            .ThenInclude(p => p.FinishedMaterial)
            .OrderByDescending(d => d.Id)
            .ToListAsync(ct);

        List<SalesGoodsIssueDocument> sales;
        try
        {
            sales = await _db.SalesGoodsIssueDocuments.AsNoTracking()
                .Include(d => d.SalesOrder)
                .OrderByDescending(d => d.Id)
                .ToListAsync(ct);
        }
        catch
        {
            sales = new List<SalesGoodsIssueDocument>();
        }

        var rows = new List<StockGoodsIssueRowVm>();
        foreach (var d in reservation)
        {
            var po = d.ProductionOrder;
            rows.Add(new StockGoodsIssueRowVm
            {
                SourceKind = StockGoodsIssueRowVm.SourceReservation,
                DocumentId = d.Id,
                DocumentNumber = d.DocumentNumber,
                DocumentDate = d.DocumentDate,
                DispatchStatus = d.DispatchStatus,
                DispatchSentAt = d.DispatchSentAt,
                DocumentWorkflowStatus = d.Status,
                SourceId = d.ProductionOrderId,
                SourceLabel = po != null ? $"PO-{po.ProductionNumber}" : $"PO id {d.ProductionOrderId}",
                DetailHint = po?.FinishedMaterialNumber
            });
        }

        foreach (var d in sales)
        {
            rows.Add(new StockGoodsIssueRowVm
            {
                SourceKind = StockGoodsIssueRowVm.SourceSalesOrder,
                DocumentId = d.Id,
                DocumentNumber = d.DocumentNumber,
                DocumentDate = d.DocumentDate,
                DispatchStatus = d.DispatchStatus,
                DispatchSentAt = d.DispatchSentAt,
                DocumentWorkflowStatus = d.Status,
                SourceId = d.SalesOrderId,
                SourceLabel = d.SalesOrder?.SalesOrderNumber ?? $"SO id {d.SalesOrderId}",
                DetailHint = d.SalesOrder?.CustomerName
            });
        }

        rows = rows.OrderByDescending(r => r.DocumentDate).ThenByDescending(r => r.DocumentId).ToList();

        rows = ApplyStockGoodsIssueFilters(rows, q, dispatch, source, dateFrom, dateTo).ToList();

        var vm = new StockGoodsIssueIndexVm
        {
            FocusProductionOrderId = productionOrderId,
            FocusSalesOrderId = salesOrderId,
            Q = q,
            Dispatch = dispatch,
            Source = source,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Rows = rows
        };
        return View(vm);
    }

    private static IEnumerable<StockGoodsIssueRowVm> ApplyStockGoodsIssueFilters(
        IEnumerable<StockGoodsIssueRowVm> rows,
        string? q,
        string? dispatch,
        string? source,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var list = rows;
        var src = (source ?? "").Trim();
        if (string.Equals(src, StockGoodsIssueRowVm.SourceReservation, StringComparison.OrdinalIgnoreCase))
            list = list.Where(r => r.SourceKind == StockGoodsIssueRowVm.SourceReservation);
        else if (string.Equals(src, StockGoodsIssueRowVm.SourceSalesOrder, StringComparison.OrdinalIgnoreCase))
            list = list.Where(r => r.SourceKind == StockGoodsIssueRowVm.SourceSalesOrder);

        var disp = (dispatch ?? "").Trim();
        if (string.Equals(disp, GoodsIssueDocument.DispatchPending, StringComparison.OrdinalIgnoreCase))
            list = list.Where(r => string.Equals(r.DispatchStatus, GoodsIssueDocument.DispatchPending, StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.DispatchStatus, SalesGoodsIssueDocument.DispatchPending, StringComparison.OrdinalIgnoreCase));
        else if (string.Equals(disp, GoodsIssueDocument.DispatchSent, StringComparison.OrdinalIgnoreCase))
            list = list.Where(r => string.Equals(r.DispatchStatus, GoodsIssueDocument.DispatchSent, StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.DispatchStatus, SalesGoodsIssueDocument.DispatchSent, StringComparison.OrdinalIgnoreCase));

        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.Date;
            list = list.Where(r => r.DocumentDate.Date >= from);
        }

        if (dateTo.HasValue)
        {
            var to = dateTo.Value.Date;
            list = list.Where(r => r.DocumentDate.Date <= to);
        }

        var search = (q ?? "").Trim();
        if (search.Length > 0)
        {
            static string Hay(StockGoodsIssueRowVm r) =>
                $"{r.DocumentNumber} {r.SourceLabel} {r.DetailHint}".Trim();
            list = list.Where(r => Hay(r).Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return list;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendReservation(
        int id,
        string? q,
        string? dispatch,
        string? source,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken ct = default)
    {
        var (ok, msg) = await _reservationGi.SendGoodsAsync(
            id,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            ct);
        if (ok) TempData["SgiHubMessage"] = msg;
        else TempData["SgiHubError"] = msg;
        return RedirectToAction(nameof(Index), new { q, dispatch, source, dateFrom, dateTo });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendSales(
        int id,
        string? q,
        string? dispatch,
        string? source,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken ct = default)
    {
        var (ok, msg) = await _salesGi.SendGoodsAsync(
            id,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            ct);
        if (ok) TempData["SgiHubMessage"] = msg;
        else TempData["SgiHubError"] = msg;
        return RedirectToAction(nameof(Index), new { q, dispatch, source, dateFrom, dateTo });
    }

    /// <summary>PDF for the linked sales order (Inventory policy so Store/Production can open from Stock GI hub).</summary>
    [HttpGet]
    public async Task<IActionResult> LinkedSalesOrderPdf(int salesOrderId, CancellationToken ct = default)
    {
        if (salesOrderId <= 0)
            return BadRequest();

        var linkedGi = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .AnyAsync(g => g.SalesOrderId == salesOrderId, ct);
        if (!linkedGi)
            return NotFound();

        var o = await _db.SalesOrders.AsNoTracking()
            .Include(x => x.Items)!.ThenInclude(i => i.QuantityUom)
            .FirstOrDefaultAsync(x => x.Id == salesOrderId, ct);
        if (o == null)
            return NotFound();

        var companyHeader = await _companyInfo.GetPdfHeaderAsync(ct).ConfigureAwait(false);
        var bytes = SalesOrderPdfService.BuildPdf(o, o.Items.OrderBy(i => i.Id).ToList(), companyHeader);
        var fileName = SafeSalesOrderPdfFileName(o.SalesOrderNumber);
        return File(bytes, "application/pdf", fileName);
    }

    private static string SafeSalesOrderPdfFileName(string? number)
    {
        var s = (number ?? "order").Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '-');
        if (string.IsNullOrEmpty(s)) s = "order";
        if (!s.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) s += ".pdf";
        return s;
    }
}
