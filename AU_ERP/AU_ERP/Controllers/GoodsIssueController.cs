using AU_ERP.Models;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AU_ERP.Controllers;

[Authorize(Policy = "ProductionDepartment")]
public class GoodsIssueController : Controller
{
    private readonly AppDbContext _db;
    private readonly GoodsIssueService _service;
    private readonly GoodsIssuePdfService _pdf;

    public GoodsIssueController(AppDbContext db, GoodsIssueService service, GoodsIssuePdfService pdf)
    {
        _db = db;
        _service = service;
        _pdf = pdf;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? productionOrderId, CancellationToken ct = default)
    {
        var docs = await _db.GoodsIssueDocuments.AsNoTracking()
            .Include(d => d.ProductionOrder!)
            .ThenInclude(p => p.FinishedMaterial)
            .OrderByDescending(d => d.Id)
            .ToListAsync(ct);

        var vm = new GoodsIssueIndexVm
        {
            FocusProductionOrderId = productionOrderId,
            Documents = docs.Select(d => new GoodsIssueRowVm(d)).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrOpen(int productionOrderId, CancellationToken ct = default)
    {
        var (success, _, goodsIssueId) = await _service.CreateOrOpenPendingAsync(
            productionOrderId,
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier),
            ct);

        if (!success || !goodsIssueId.HasValue)
            return RedirectToAction(nameof(Index));

        return RedirectToAction(nameof(Index), new { productionOrderId });
    }

    [HttpGet]
    public async Task<JsonResult> GetDocument(int id, CancellationToken ct = default)
    {
        if (id <= 0)
            return Json(new { success = false, message = "Invalid request." });

        var doc = await _db.GoodsIssueDocuments.AsNoTracking()
            .Include(d => d.ProductionOrder!)
            .ThenInclude(p => p.FinishedMaterial)
            .Include(d => d.Lines)
            .ThenInclude(l => l.RequiredUom)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc == null)
            return Json(new { success = false, message = "Goods issue not found." });

        return Json(new
        {
            success = true,
            data = new
            {
                id = doc.Id,
                documentNumber = doc.DocumentNumber,
                documentDate = doc.DocumentDate.ToString("yyyy-MM-dd"),
                status = doc.Status,
                dispatchStatus = doc.DispatchStatus,
                dispatchSentAt = doc.DispatchSentAt?.ToString("yyyy-MM-dd HH:mm") ?? "",
                productionNumber = doc.ProductionOrder?.ProductionNumber,
                materialNumber = doc.ProductionOrder?.FinishedMaterialNumber,
                materialDescription = doc.ProductionOrder?.FinishedMaterial?.Description,
                lines = doc.Lines.OrderBy(l => l.Id).Select(l => new
                {
                    id = l.Id,
                    productionOrderLineId = l.ProductionOrderLineId,
                    fertMaterialNumber = l.FertMaterialNumber,
                    fertMaterialDescription = l.FertMaterialDescription,
                    materialNumber = l.MaterialNumber,
                    materialDescription = l.MaterialDescription,
                    requiredQty = l.RequiredQty,
                    issuedQty = l.IssuedQty,
                    remainingQty = l.RemainingQty,
                    uomCode = l.RequiredUom != null ? l.RequiredUom.Code : "—"
                })
            }
        });
    }

    [HttpPost]
    public async Task<JsonResult> SendGoods(int id, CancellationToken ct = default)
    {
        var result = await _service.SendGoodsAsync(
            id,
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier),
            ct);
        return Json(new { success = result.success, message = result.message });
    }

    [HttpPost]
    public async Task<JsonResult> ReceiveGoods(int id, [FromBody] GoodsIssueReceiveDto? dto, CancellationToken ct = default)
    {
        IReadOnlyDictionary<int, decimal>? reqByLine = null;
        if (dto?.Lines != null && dto.Lines.Count > 0)
            reqByLine = dto.Lines
                .Where(x => x.LineId > 0 && x.IssueQty >= 0)
                .GroupBy(x => x.LineId)
                .ToDictionary(g => g.Key, g => g.Last().IssueQty);
        var result = await _service.ReceiveGoodsAsync(
            id,
            User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier),
            reqByLine,
            ct);
        return Json(new { success = result.success, message = result.message });
    }

    [HttpGet]
    public async Task<IActionResult> View(int id, CancellationToken ct = default)
    {
        if (id <= 0)
            return BadRequest();

        var doc = await _db.GoodsIssueDocuments.AsNoTracking()
            .Include(d => d.ProductionOrder!)
            .ThenInclude(p => p.FinishedMaterial)
            .Include(d => d.Lines)
            .ThenInclude(l => l.RequiredUom)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc == null)
            return NotFound();

        return PartialView("_GoodsIssueDetails", doc);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken ct = default)
    {
        if (id <= 0)
            return BadRequest();

        var doc = await _db.GoodsIssueDocuments.AsNoTracking()
            .Include(d => d.ProductionOrder!)
            .ThenInclude(p => p.FinishedMaterial)
            .Include(d => d.Lines)
            .ThenInclude(l => l.RequiredUom)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc == null)
            return NotFound();

        var bytes = await _pdf.BuildPdfAsync(doc, ct);
        var fileName = $"GI_{(doc.DocumentNumber?.Trim().Length > 0 ? doc.DocumentNumber.Trim() : doc.Id.ToString())}.pdf";
        return File(bytes, "application/pdf", fileName);
    }
}

public class GoodsIssueReceiveDto
{
    public List<GoodsIssueReceiveLineDto> Lines { get; set; } = new();
}

public class GoodsIssueReceiveLineDto
{
    public int LineId { get; set; }
    public decimal IssueQty { get; set; }
}
