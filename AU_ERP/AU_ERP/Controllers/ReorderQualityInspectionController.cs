using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "SalesDepartment")]
public class ReorderQualityInspectionController : Controller
{
    private readonly AppDbContext _db;
    private readonly SalesReturnQiBomService _bom;
    private readonly SalesReturnQiPostingService _posting;

    public ReorderQualityInspectionController(
        AppDbContext db,
        SalesReturnQiBomService bom,
        SalesReturnQiPostingService posting)
    {
        _db = db;
        _bom = bom;
        _posting = posting;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewData["Title"] = "Reorder quality inspection";
        List<ReorderQiIndexRowVm> rows;
        try
        {
            rows = await (
                from qi in _db.SalesReturnQualityInspections.AsNoTracking()
                join ro in _db.SalesReturnOrders.AsNoTracking() on qi.SalesReturnOrderId equals ro.Id
                orderby qi.DocumentDate descending, qi.Id descending
                select new ReorderQiIndexRowVm
                {
                    QiId = qi.Id,
                    DocumentNumber = qi.DocumentNumber,
                    DocumentDate = qi.DocumentDate,
                    Status = qi.Status,
                    InvoiceDocumentNumber = ro.InvoiceDocumentNumber,
                    ReturnOrderDocumentNumber = ro.DocumentNumber
                }).ToListAsync(ct);
        }
        catch (SqlException ex) when (
            ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) &&
            ex.Message.Contains("SalesReturnQualityInspections", StringComparison.OrdinalIgnoreCase))
        {
            TempData["RqiError"] = "Quality inspection tables are missing. Run database migrations.";
            rows = new List<ReorderQiIndexRowVm>();
        }

        return View(new ReorderQiIndexVm { Items = rows });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        var qi = await _db.SalesReturnQualityInspections.AsNoTracking()
            .Include(q => q.Lines).ThenInclude(l => l.QuantityUom)
            .Include(q => q.SalesReturnOrder)
            .FirstOrDefaultAsync(q => q.Id == id, ct);
        if (qi == null)
            return NotFound();

        var ro = qi.SalesReturnOrder;
        if (ro == null)
            return NotFound();

        var lines = new List<ReorderQiDetailsLineVm>();
        foreach (var ln in qi.Lines.OrderBy(l => l.Id))
        {
            var rawList = await _bom.ListRawComponentMaterialNumbersAsync(ln.MaterialNumber, ct).ConfigureAwait(false);
            lines.Add(new ReorderQiDetailsLineVm
            {
                LineId = ln.Id,
                MaterialNumber = ln.MaterialNumber,
                MaterialDescription = ln.MaterialDescription,
                BatchNumber = ln.BatchNumber,
                QuantityReturned = ln.QuantityReturned,
                UomCode = ln.QuantityUom?.Code,
                UnitPrice = ln.UnitPrice,
                ItemChargeValuesJson = ln.ItemChargeValuesJson,
                QtyBackToStock = ln.QtyBackToStock,
                QtyConvertToRaw = ln.QtyConvertToRaw,
                QtyScrap = ln.QtyScrap,
                HasBomRoh = rawList.Count > 0
            });
        }

        var vm = new ReorderQiDetailsVm
        {
            QiId = qi.Id,
            DocumentNumber = qi.DocumentNumber,
            DocumentDate = qi.DocumentDate,
            Status = qi.Status,
            PlantId = qi.PlantId,
            InvoiceDocumentNumber = ro.InvoiceDocumentNumber,
            ReturnOrderDocumentNumber = ro.DocumentNumber,
            Lines = lines
        };

        ViewData["Title"] = $"QI {qi.DocumentNumber}";
        return View(vm);
    }

    public sealed class QiLineDispositionFormRow
    {
        public int LineId { get; set; }
        public decimal QtyBackToStock { get; set; }
        public decimal QtyConvertToRaw { get; set; }
        public decimal QtyScrap { get; set; }
    }

    public sealed class QiCompleteForm
    {
        public int QiId { get; set; }
        public List<QiLineDispositionFormRow>? Lines { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete([FromForm] QiCompleteForm model, CancellationToken ct = default)
    {
        if (model.QiId <= 0)
        {
            TempData["RqiError"] = "Invalid request.";
            return RedirectToAction(nameof(Index));
        }

        var qi = await _db.SalesReturnQualityInspections.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == model.QiId, ct);
        if (qi == null)
            return NotFound();

        if (!string.Equals(qi.Status, SalesReturnQualityInspection.StatusPending, StringComparison.OrdinalIgnoreCase))
        {
            TempData["RqiError"] = "This inspection is not pending.";
            return RedirectToAction(nameof(Details), new { id = model.QiId });
        }

        var inputs = (model.Lines ?? new List<QiLineDispositionFormRow>())
            .Select(l => new SalesReturnQiPostingService.QiDispositionInput(
                l.LineId,
                l.QtyBackToStock,
                l.QtyConvertToRaw,
                l.QtyScrap))
            .ToList();

        var result = await _posting.CompleteAsync(model.QiId, inputs, ct).ConfigureAwait(false);
        if (!result.Success)
        {
            TempData["RqiError"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = model.QiId });
        }

        TempData["RqiMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = model.QiId });
    }
}
