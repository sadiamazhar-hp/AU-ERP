using AU_ERP.Models;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AU_ERP.Controllers;

[Authorize(Policy = "ProductionDepartment")]
public class GoodReceiptController : Controller
{
    private readonly AppDbContext _db;
    private readonly GoodsReceiptPostingService _posting;
    private readonly GoodReceiptPdfService _pdf;

    public GoodReceiptController(AppDbContext db, GoodsReceiptPostingService posting, GoodReceiptPdfService pdf)
    {
        _db = db;
        _posting = posting;
        _pdf = pdf;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? productionOrderId, CancellationToken ct = default)
    {
        if (productionOrderId.HasValue && productionOrderId.Value > 0)
            await EnsureDraftAsync(productionOrderId.Value, ct).ConfigureAwait(false);

        var docs = await _db.GoodReceiptDocuments.AsNoTracking()
            .Include(d => d.ProductionOrder!)
            .ThenInclude(p => p.FinishedMaterial)
            .OrderByDescending(d => d.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var vm = new GoodReceiptIndexVm
        {
            FocusProductionOrderId = productionOrderId,
            Documents = docs.Select(d => new GoodReceiptRowVm(d)).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrOpen(int productionOrderId, CancellationToken ct = default)
    {
        if (productionOrderId <= 0)
            return BadRequest("Invalid production order.");

        await EnsureDraftAsync(productionOrderId, ct).ConfigureAwait(false);
        return RedirectToAction(nameof(Index), new { productionOrderId });
    }

    [HttpPost]
    public async Task<JsonResult> SaveDraft([FromBody] GoodReceiptDraftSaveDto? dto, CancellationToken ct = default)
    {
        if (dto == null || dto.Id <= 0)
            return Json(new { success = false, message = "Invalid request." });

        var doc = await _db.GoodReceiptDocuments.FirstOrDefaultAsync(d => d.Id == dto.Id, ct).ConfigureAwait(false);
        if (doc == null)
            return Json(new { success = false, message = "Good receipt document not found." });

        if (doc.IsPosted)
            return Json(new { success = false, message = "Already posted." });

        doc.DocumentDate = dto.DocumentDate.Date;
        doc.DocumentNumber = (dto.DocumentNumber ?? "").Trim();
        doc.BatchNo = (dto.BatchNo ?? "").Trim();
        doc.ProducedQty = dto.ProducedQty;
        doc.QtyFirstQuality = dto.QtyFirstQuality;
        doc.QtySecondQuality = dto.QtySecondQuality;
        doc.QtyThirdQuality = dto.QtyThirdQuality;
        doc.RejectedScrapQty = dto.RejectedScrapQty;
        doc.DraftLinesJson = string.IsNullOrWhiteSpace(dto.DraftLinesJson) ? null : dto.DraftLinesJson;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Json(new { success = true });
    }

    /// <summary>
    /// Allows correcting posted-document metadata (date / number / batch) without changing posted quantities.
    /// </summary>
    [HttpPost]
    public async Task<JsonResult> UpdatePostedMeta([FromBody] GoodReceiptDraftSaveDto? dto, CancellationToken ct = default)
    {
        if (dto == null || dto.Id <= 0)
            return Json(new { success = false, message = "Invalid request." });

        var doc = await _db.GoodReceiptDocuments.FirstOrDefaultAsync(d => d.Id == dto.Id, ct).ConfigureAwait(false);
        if (doc == null)
            return Json(new { success = false, message = "Good receipt document not found." });

        if (!doc.IsPosted)
            return Json(new { success = false, message = "This document is not posted yet." });

        doc.DocumentDate = dto.DocumentDate.Date;
        doc.DocumentNumber = (dto.DocumentNumber ?? "").Trim();
        doc.BatchNo = (dto.BatchNo ?? "").Trim();

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<JsonResult> GetDraft(int id, CancellationToken ct = default)
    {
        if (id <= 0)
            return Json(new { success = false, message = "Invalid request." });

        var d = await _db.GoodReceiptDocuments.AsNoTracking()
            .Include(x => x.ProductionOrder!)
            .ThenInclude(p => p.FinishedMaterial)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);
        if (d == null)
            return Json(new { success = false, message = "Good receipt document not found." });

        return Json(new
        {
            success = true,
            data = new
            {
                id = d.Id,
                documentDate = d.DocumentDate.ToString("yyyy-MM-dd"),
                documentNumber = d.DocumentNumber,
                batchNo = d.BatchNo,
                producedQty = d.ProducedQty,
                qtyFirstQuality = d.QtyFirstQuality,
                qtySecondQuality = d.QtySecondQuality,
                qtyThirdQuality = d.QtyThirdQuality,
                rejectedScrapQty = d.RejectedScrapQty,
                draftLinesJson = d.DraftLinesJson,
                isPosted = d.IsPosted,
                productionNumber = d.ProductionOrder?.ProductionNumber,
                materialNumber = d.ProductionOrder?.FinishedMaterialNumber,
                materialDescription = d.ProductionOrder?.FinishedMaterial?.Description
            }
        });
    }

    [HttpPost]
    public async Task<JsonResult> PostReceipt(int id, CancellationToken ct = default)
    {
        if (id <= 0)
            return Json(new { success = false, message = "Invalid request." });

        var doc = await _db.GoodReceiptDocuments.FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false);
        if (doc == null)
            return Json(new { success = false, message = "Good receipt document not found." });

        var post = new GoodsReceiptPostDto
        {
            ProductionOrderId = doc.ProductionOrderId,
            GrDate = doc.DocumentDate,
            ProducedQty = doc.ProducedQty,
            QtyFirstQuality = doc.QtyFirstQuality,
            QtySecondQuality = doc.QtySecondQuality,
            QtyThirdQuality = doc.QtyThirdQuality,
            RejectedScrapQty = doc.RejectedScrapQty,
            BatchNo = doc.BatchNo,
            Lines = ParseDraftLines(doc.DraftLinesJson)
        };

        var result = await _posting.PostAsync(post, ct).ConfigureAwait(false);
        if (!result.Success)
            return Json(new { success = false, message = result.Message });

        doc.IsPosted = true;
        doc.PostedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Json(new { success = true, message = result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> View(int id, CancellationToken ct = default)
    {
        if (id <= 0)
            return BadRequest();

        var doc = await _db.GoodReceiptDocuments.AsNoTracking()
            .Include(d => d.ProductionOrder!)
            .ThenInclude(p => p.FinishedMaterial)
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            .ConfigureAwait(false);

        if (doc == null)
            return NotFound();

        return PartialView("_GoodReceiptDetails", new GoodReceiptRowVm(doc));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken ct = default)
    {
        if (id <= 0)
            return BadRequest();

        var doc = await _db.GoodReceiptDocuments.AsNoTracking()
            .Include(d => d.ProductionOrder!)
            .ThenInclude(p => p.FinishedMaterial)
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            .ConfigureAwait(false);

        if (doc == null)
            return NotFound();

        var bytes = await _pdf.BuildPdfAsync(doc, ct).ConfigureAwait(false);
        var fileName = $"GR_{(doc.DocumentNumber?.Trim().Length > 0 ? doc.DocumentNumber.Trim() : doc.Id.ToString())}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    private async Task EnsureDraftAsync(int productionOrderId, CancellationToken ct)
    {
        var exists = await _db.GoodReceiptDocuments.AnyAsync(d => d.ProductionOrderId == productionOrderId, ct).ConfigureAwait(false);
        if (exists)
            return;

        var po = await _db.ProductionOrders.AsNoTracking()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == productionOrderId, ct)
            .ConfigureAwait(false);
        if (po == null)
            return;

        var lastOut = await _db.ProductionOrderStageProgresses.AsNoTracking()
            .Where(s => s.ProductionOrderId == productionOrderId && s.OutputQuantity.HasValue)
            .OrderByDescending(s => s.SequenceOrder)
            .Select(s => s.OutputQuantity)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var poLines = po.Lines.OrderBy(l => l.LineNo).ToList();
        if (poLines.Count == 0)
        {
            poLines.Add(new ProductionOrderLine
            {
                Id = 0,
                ProductionOrderId = po.Id,
                LineNo = 1,
                MaterialNumber = po.FinishedMaterialNumber,
                PlannedQuantity = po.TargetQuantity,
                UomId = po.UomId
            });
        }
        var defaultProduced = lastOut ?? poLines.Sum(l => l.PlannedQuantity);
        var docNo = await GenerateNextGoodReceiptNumberAsync(ct).ConfigureAwait(false);
        var batchNo = await GenerateNextBatchNumberAsync(ct).ConfigureAwait(false);
        var lines = poLines.Select(l => new GoodsReceiptPostLineDto
        {
            ProductionOrderLineId = l.Id,
            MaterialNumber = l.MaterialNumber,
            UomId = l.UomId,
            ProducedQty = l.PlannedQuantity,
            QtyFirstQuality = l.PlannedQuantity,
            QtySecondQuality = 0m,
            QtyThirdQuality = 0m,
            RejectedScrapQty = 0m
        }).ToList();

        _db.GoodReceiptDocuments.Add(new GoodReceiptDocument
        {
            ProductionOrderId = productionOrderId,
            DocumentDate = DateTime.Today,
            DocumentNumber = docNo,
            BatchNo = batchNo,
            ProducedQty = defaultProduced,
            QtyFirstQuality = defaultProduced,
            QtySecondQuality = 0,
            QtyThirdQuality = 0,
            RejectedScrapQty = 0,
            DraftLinesJson = JsonSerializer.Serialize(lines),
            IsPosted = false,
            PostedAt = null,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static List<GoodsReceiptPostLineDto> ParseDraftLines(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<GoodsReceiptPostLineDto>();
        try
        {
            var rows = JsonSerializer.Deserialize<List<GoodsReceiptPostLineDto>>(json);
            return rows ?? new List<GoodsReceiptPostLineDto>();
        }
        catch
        {
            return new List<GoodsReceiptPostLineDto>();
        }
    }

    private async Task<string> GenerateNextGoodReceiptNumberAsync(CancellationToken ct)
    {
        const string prefix = "GR";
        const int start = 1500;

        var nums = await _db.GoodReceiptDocuments.AsNoTracking()
            .Select(d => d.DocumentNumber)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var max = 0;
        foreach (var s in nums)
        {
            var t = (s ?? "").Trim();
            if (t.Length < prefix.Length + 1) continue;
            if (!t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var tail = t.Substring(prefix.Length).Trim();
            if (int.TryParse(tail, out var n))
                max = Math.Max(max, n);
        }

        var next = Math.Max(start, max + 1);
        return $"{prefix}{next}";
    }

    private async Task<string> GenerateNextBatchNumberAsync(CancellationToken ct)
    {
        const string prefix = "BTH-";
        const int start = 100;

        var nums = await _db.GoodReceiptDocuments.AsNoTracking()
            .Select(d => d.BatchNo)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var max = 0;
        foreach (var s in nums)
        {
            var t = (s ?? "").Trim();
            if (t.Length < prefix.Length + 1) continue;
            if (!t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var tail = t.Substring(prefix.Length).Trim();
            if (int.TryParse(tail, out var n))
                max = Math.Max(max, n);
        }

        var next = Math.Max(start, max + 1);
        return $"{prefix}{next}";
    }
}

