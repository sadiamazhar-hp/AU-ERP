using AU_ERP.Configuration;
using AU_ERP.Models;
using AU_ERP.Services;
using AU_ERP.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace AU_ERP.Controllers;

[Authorize(Policy = "ProductionDepartment")]
public class GoodReceiptController : Controller
{
    private readonly AppDbContext _db;
    private readonly GoodsReceiptPostingService _posting;
    private readonly GoodReceiptPdfService _pdf;
    private readonly DocumentNumberAllocator _documentNumbers;

    public GoodReceiptController(
        AppDbContext db,
        GoodsReceiptPostingService posting,
        GoodReceiptPdfService pdf,
        DocumentNumberAllocator documentNumbers)
    {
        _db = db;
        _posting = posting;
        _pdf = pdf;
        _documentNumbers = documentNumbers;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? productionOrderId, CancellationToken ct = default)
    {
        if (productionOrderId.HasValue && productionOrderId.Value > 0)
        {
            var (ok, err) = await EnsureDraftAsync(productionOrderId.Value, ct).ConfigureAwait(false);
            if (!ok && !string.IsNullOrWhiteSpace(err))
                TempData["GoodReceiptError"] = err;
        }

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

        var (ok, err) = await EnsureDraftAsync(productionOrderId, ct).ConfigureAwait(false);
        if (!ok && !string.IsNullOrWhiteSpace(err))
            TempData["GoodReceiptError"] = err;
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

        foreach (var (label, qty) in new (string Label, decimal Qty)[]
        {
            ("Produced quantity", dto.ProducedQty),
            ("First-quality quantity", dto.QtyFirstQuality),
            ("Second-quality quantity", dto.QtySecondQuality),
            ("Third-quality quantity", dto.QtyThirdQuality),
            ("Rejected/scrap quantity", dto.RejectedScrapQty)
        })
        {
            var fracErr = DocumentQuantityRules.ValidateNonNegativeWhole(qty, label);
            if (fracErr != null)
                return Json(new { success = false, message = fracErr });
        }

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

    private async Task<(bool Ok, string? Error)> EnsureDraftAsync(int productionOrderId, CancellationToken ct)
    {
        var exists = await _db.GoodReceiptDocuments.AnyAsync(d => d.ProductionOrderId == productionOrderId, ct).ConfigureAwait(false);
        if (exists)
            return (true, null);

        var po = await _db.ProductionOrders.AsNoTracking()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == productionOrderId, ct)
            .ConfigureAwait(false);
        if (po == null)
            return (true, null);

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

        try
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx =
                    await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct).ConfigureAwait(false);
                try
                {
                    var docNo = await _documentNumbers.AllocateAsync(ModuleKeys.QualityInspection, ct).ConfigureAwait(false);
                    var batchNo = await _documentNumbers.AllocateAsync(ModuleKeys.Batch, ct).ConfigureAwait(false);

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
                    await tx.CommitAsync(ct).ConfigureAwait(false);
                }
                catch
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    throw;
                }
            }).ConfigureAwait(false);
            return (true, null);
        }
        catch (DocumentIntegrationMissingException ex)
        {
            return (false, ex.Message);
        }
        catch (DocumentIntegrationRangeExhaustedException ex)
        {
            return (false, ex.Message);
        }
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

}

