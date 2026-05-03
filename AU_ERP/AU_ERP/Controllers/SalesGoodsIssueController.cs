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
    public async Task<IActionResult> Index(int? salesOrderId, CancellationToken ct = default)
    {
        List<SalesGoodsIssueDocument> docs;
        try
        {
            docs = await _db.SalesGoodsIssueDocuments.AsNoTracking()
                .Include(d => d.SalesOrder)
                .OrderByDescending(d => d.Id)
                .ToListAsync(ct);
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
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrOpen(int salesOrderId, CancellationToken ct = default)
    {
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
        var doc = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .Include(d => d.SalesOrder)
            .Include(d => d.Lines)
            .ThenInclude(l => l.RequiredUom)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc == null)
            return NotFound();
        return PartialView("_SalesGoodsIssueDetails", doc);
    }
}
