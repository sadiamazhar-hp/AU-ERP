using AU_ERP.Models;
using AU_ERP.Services;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AU_ERP.Controllers;

[Authorize(Policy = "SalesDepartment")]
public class SalesGoodsIssueController : Controller
{
    private readonly AppDbContext _db;
    private readonly SalesGoodsIssueService _service;

    public SalesGoodsIssueController(AppDbContext db, SalesGoodsIssueService service)
    {
        _db = db;
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? salesOrderId, string? plantId, CancellationToken ct = default)
    {
        var allPlants = await _db.PlantsSamples.AsNoTracking().OrderBy(p => p.PlantName).ToListAsync(ct).ConfigureAwait(false);
        var plantScope = await SalesPlantAccess.ResolveAsync(_db, User, plantId, allPlants.Select(p => p.PlantID), ct).ConfigureAwait(false);
        SalesPlantAccess.SetViewBag(this, plantScope, allPlants);

        List<SalesGoodsIssueDocument> docs;
        try
        {
            var joined = from doc in _db.SalesGoodsIssueDocuments.AsNoTracking()
                join so in _db.SalesOrders.AsNoTracking() on doc.SalesOrderId equals so.Id
                select new { doc, PlantId = so.PlantId };
            joined = SalesPlantAccess.ApplyListingPlantFilter(joined, plantScope, x => x.PlantId);
            var ids = await joined
                .OrderByDescending(x => x.doc.Id)
                .Select(x => x.doc.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            docs = ids.Count == 0
                ? new List<SalesGoodsIssueDocument>()
                : await _db.SalesGoodsIssueDocuments.AsNoTracking()
                    .Include(d => d.SalesOrder)
                    .Where(d => ids.Contains(d.Id))
                    .OrderByDescending(d => d.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
        }
        catch (SqlException ex) when (
            ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) &&
            ex.Message.Contains("SalesGoodsIssueDocuments", StringComparison.OrdinalIgnoreCase))
        {
            TempData["SgiError"] = "Sales GI tables are missing in the current database. Run database migrations for this environment.";
            docs = new List<SalesGoodsIssueDocument>();
        }

        var vm = new SalesGoodsIssueIndexVm
        {
            FocusSalesOrderId = salesOrderId,
            Documents = docs.Select(d => new SalesGoodsIssueRowVm(d)).ToList()
        };
        ViewBag.FilterPlantId = plantScope.EffectiveListPlantId;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrOpen(int salesOrderId, CancellationToken ct = default)
    {
        if (!await IsSalesOrderPlantAllowedAsync(salesOrderId, ct).ConfigureAwait(false))
        {
            TempData["SgiError"] = "You are not allowed to access this sales order's plant.";
            return RedirectToAction(nameof(Index), new { salesOrderId });
        }

        var (ok, msg, _) = await _service.CreateOrOpenPendingAsync(
            salesOrderId,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            ct);
        if (ok) TempData["SgiMessage"] = msg;
        else TempData["SgiError"] = msg;
        return RedirectToAction(nameof(Index), new { salesOrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int id, CancellationToken ct = default)
    {
        if (!await IsGoodsIssueReadableAsync(id, ct).ConfigureAwait(false))
        {
            TempData["SgiError"] = "You are not allowed to access this goods issue document.";
            return RedirectToAction(nameof(Index));
        }

        var (ok, msg) = await _service.SendGoodsAsync(
            id,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            ct);
        if (ok) TempData["SgiMessage"] = msg;
        else TempData["SgiError"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int id, CancellationToken ct = default)
    {
        if (!await IsGoodsIssueReadableAsync(id, ct).ConfigureAwait(false))
        {
            TempData["SgiError"] = "You are not allowed to access this goods issue document.";
            return RedirectToAction(nameof(Index));
        }

        var (ok, msg) = await _service.ReceiveGoodsAsync(
            id,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            ct);
        if (ok) TempData["SgiMessage"] = msg;
        else TempData["SgiError"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> View(int id, CancellationToken ct = default)
    {
        if (!await IsGoodsIssueReadableAsync(id, ct).ConfigureAwait(false))
            return NotFound();

        var doc = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .Include(d => d.SalesOrder)
            .Include(d => d.Lines)
            .ThenInclude(l => l.RequiredUom)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc == null)
            return NotFound();
        return PartialView("_SalesGoodsIssueDetails", doc);
    }

    private async Task<bool> IsSalesOrderPlantAllowedAsync(int salesOrderId, CancellationToken ct)
    {
        var plantId = await _db.SalesOrders.AsNoTracking()
            .Where(o => o.Id == salesOrderId)
            .Select(o => o.PlantId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        var allPlantIds = await _db.PlantsSamples.AsNoTracking()
            .Select(p => p.PlantID)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var scope = await SalesPlantAccess.ResolveAsync(_db, User, null, allPlantIds, ct).ConfigureAwait(false);
        return SalesPlantAccess.IsPlantReadable(scope, plantId);
    }

    private async Task<bool> IsGoodsIssueReadableAsync(int goodsIssueId, CancellationToken ct)
    {
        var plantId = await (
            from doc in _db.SalesGoodsIssueDocuments.AsNoTracking()
            join so in _db.SalesOrders.AsNoTracking() on doc.SalesOrderId equals so.Id
            where doc.Id == goodsIssueId
            select so.PlantId).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var allPlantIds = await _db.PlantsSamples.AsNoTracking()
            .Select(p => p.PlantID)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var scope = await SalesPlantAccess.ResolveAsync(_db, User, null, allPlantIds, ct).ConfigureAwait(false);
        return SalesPlantAccess.IsPlantReadable(scope, plantId);
    }
}
