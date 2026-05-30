using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using AU_ERP.Validation;
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
    public async Task<IActionResult> Index(string? plantId, CancellationToken ct = default)
    {
        ViewData["Title"] = "ROD Inspection";
        var allPlants = await _db.PlantsSamples.AsNoTracking().OrderBy(p => p.PlantName).ToListAsync(ct).ConfigureAwait(false);
        var plantScope = await SalesPlantAccess.ResolveAsync(_db, User, plantId, allPlants.Select(p => p.PlantID), ct).ConfigureAwait(false);
        SalesPlantAccess.SetViewBag(this, plantScope, allPlants);
        List<ReorderQiIndexRowVm> rows;
        try
        {
            var joined = from qi in _db.SalesReturnQualityInspections.AsNoTracking()
                join ro in _db.SalesReturnOrders.AsNoTracking() on qi.SalesReturnOrderId equals ro.Id
                select new { qi, ro };
            var plantFiltered = SalesPlantAccess.ApplyListingPlantFilter(joined, plantScope, x => x.qi.PlantId);
            rows = await plantFiltered
                .OrderByDescending(x => x.qi.DocumentDate)
                .ThenByDescending(x => x.qi.Id)
                .Select(x => new ReorderQiIndexRowVm
                {
                    QiId = x.qi.Id,
                    DocumentNumber = x.qi.DocumentNumber,
                    DocumentDate = x.qi.DocumentDate,
                    Status = x.qi.Status,
                    InvoiceDocumentNumber = x.ro.InvoiceDocumentNumber,
                    ReturnOrderDocumentNumber = x.ro.DocumentNumber
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

    private async Task<bool> IsQiReadableAsync(int qiId, CancellationToken ct)
    {
        var plantId = await _db.SalesReturnQualityInspections.AsNoTracking()
            .Where(q => q.Id == qiId)
            .Select(q => q.PlantId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        var allPlantIds = await _db.PlantsSamples.AsNoTracking()
            .Select(p => p.PlantID)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var scope = await SalesPlantAccess.ResolveAsync(_db, User, null, allPlantIds, ct).ConfigureAwait(false);
        return SalesPlantAccess.IsPlantReadable(scope, plantId);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        if (!await IsQiReadableAsync(id, ct).ConfigureAwait(false))
            return NotFound();
        var vm = await BuildDetailsVmAsync(id, ct).ConfigureAwait(false);
        if (vm == null)
            return NotFound();

        ViewData["Title"] = $"QI {vm.DocumentNumber}";
        return View(vm);
    }

    /// <summary>HTML fragment for the read-only view modal (completed inspections only).</summary>
    [HttpGet]
    public async Task<IActionResult> ReadOnlyPartial(int id, CancellationToken ct = default)
    {
        if (!await IsQiReadableAsync(id, ct).ConfigureAwait(false))
            return NotFound();
        var vm = await BuildDetailsVmAsync(id, ct).ConfigureAwait(false);
        if (vm == null)
            return NotFound();
        if (string.Equals(vm.Status, SalesReturnQualityInspection.StatusPending, StringComparison.OrdinalIgnoreCase))
            return BadRequest("Inspection is still pending; use Open to complete it.");

        return PartialView("_ReorderQiReadOnlyModalBody", vm);
    }

    private async Task<ReorderQiDetailsVm?> BuildDetailsVmAsync(int id, CancellationToken ct)
    {
        var qi = await _db.SalesReturnQualityInspections.AsNoTracking()
            .Include(q => q.Lines).ThenInclude(l => l.QuantityUom)
            .Include(q => q.SalesReturnOrder)
            .FirstOrDefaultAsync(q => q.Id == id, ct)
            .ConfigureAwait(false);
        if (qi?.SalesReturnOrder is not { } ro)
            return null;

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

        return new ReorderQiDetailsVm
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

        if (!await IsQiReadableAsync(model.QiId, ct).ConfigureAwait(false))
            return NotFound();

        if (!string.Equals(qi.Status, SalesReturnQualityInspection.StatusPending, StringComparison.OrdinalIgnoreCase))
        {
            TempData["RqiError"] = "This inspection is not pending.";
            return RedirectToAction(nameof(Details), new { id = model.QiId });
        }

        var inputsRaw = model.Lines ?? new List<QiLineDispositionFormRow>();
        foreach (var l in inputsRaw)
        {
            foreach (var (label, qty) in new (string Label, decimal Qty)[]
            {
                ("Quantity back to stock", l.QtyBackToStock),
                ("Quantity to convert to raw", l.QtyConvertToRaw),
                ("Scrap quantity", l.QtyScrap)
            })
            {
                if (DocumentQuantityRules.ValidateNonNegativeWhole(qty, $"{label} (line id {l.LineId})") is { } qiErr)
                {
                    TempData["RqiError"] = qiErr;
                    return RedirectToAction(nameof(Details), new { id = model.QiId });
                }
            }
        }

        var inputs = inputsRaw
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
