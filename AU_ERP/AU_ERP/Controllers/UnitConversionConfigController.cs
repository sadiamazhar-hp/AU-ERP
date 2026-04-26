using AU_ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "AdminDepartment")]
public class UnitConversionConfigController : Controller
{
    private readonly AppDbContext _db;

    public UnitConversionConfigController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var list = await _db.GlobalUnitConversions.AsNoTracking()
            .Include(x => x.BaseUnit)
            .Include(x => x.AltUnit)
            .OrderBy(x => x.BaseUnit!.Code)
            .ThenBy(x => x.AltUnit!.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var uoms = await _db.UnitOfMeasurements.AsNoTracking()
            .OrderBy(u => u.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        ViewBag.Uoms = uoms;
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(List<GlobalUnitConversionFormRow>? items, CancellationToken ct = default)
    {
        var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);

        if (items == null || items.Count == 0)
        {
            if (isAjax) return Json(new { success = false, message = "No data received." });
            return RedirectToAction(nameof(Index));
        }

        var toProcess = new List<GlobalUnitConversionFormRow>();
        foreach (var x in items)
        {
            if (x == null) continue;
            if (x.Id > 0)
            {
                toProcess.Add(x);
                continue;
            }
            if (x.BaseUnitId <= 0 || x.AltUnitId <= 0)
                continue;
            if (x.Quantity <= 0)
                continue;
            toProcess.Add(x);
        }

        if (toProcess.Count == 0)
        {
            if (isAjax) return Json(new { success = false, message = "No rows to save." });
            return RedirectToAction(nameof(Index));
        }

        var validUomIds = new HashSet<int>(await _db.UnitOfMeasurements.AsNoTracking().Select(u => u.Id).ToListAsync(ct).ConfigureAwait(false));

        foreach (var it in toProcess)
        {
            if (!validUomIds.Contains(it.BaseUnitId) || !validUomIds.Contains(it.AltUnitId))
            {
                if (isAjax) return Json(new { success = false, message = "Invalid unit of measure." });
                TempData["GucError"] = "Invalid unit of measure.";
                return RedirectToAction(nameof(Index));
            }
            if (it.BaseUnitId == it.AltUnitId)
            {
                if (isAjax) return Json(new { success = false, message = "Unit and alternate unit must be different." });
                TempData["GucError"] = "Unit and alternate unit must be different.";
                return RedirectToAction(nameof(Index));
            }
            if (it.Quantity <= 0)
            {
                if (isAjax) return Json(new { success = false, message = "Quantity must be greater than zero (1 [unit] = Qty of [alt])." });
                TempData["GucError"] = "Quantity must be greater than zero.";
                return RedirectToAction(nameof(Index));
            }
        }

        var keyPairs = toProcess.Select(x => (x.BaseUnitId, x.AltUnitId)).ToList();
        if (keyPairs.Count != keyPairs.Distinct().Count())
        {
            if (isAjax) return Json(new { success = false, message = "Duplicate base / alternate pairs in the table." });
            TempData["GucError"] = "Duplicate base / alternate pairs in the table.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            foreach (var it in toProcess)
            {
                var taken = await _db.GlobalUnitConversions.AsNoTracking()
                    .AnyAsync(x => x.BaseUnitId == it.BaseUnitId && x.AltUnitId == it.AltUnitId && x.Id != it.Id, ct)
                    .ConfigureAwait(false);
                if (taken)
                {
                    if (isAjax) return Json(new { success = false, message = "This unit / alternate unit pair already exists." });
                    TempData["GucError"] = "This unit / alternate unit pair already exists.";
                    return RedirectToAction(nameof(Index));
                }
            }

            foreach (var it in toProcess)
            {
                if (it.Id > 0)
                {
                    var row = await _db.GlobalUnitConversions.FindAsync(new object?[] { it.Id }, ct).ConfigureAwait(false);
                    if (row == null)
                    {
                        if (isAjax) return Json(new { success = false, message = "A row was removed. Refresh the page." });
                        TempData["GucError"] = "A row was removed. Refresh the page.";
                        return RedirectToAction(nameof(Index));
                    }
                    row.BaseUnitId = it.BaseUnitId;
                    row.AltUnitId = it.AltUnitId;
                    row.Quantity = it.Quantity;
                }
                else
                {
                    _db.GlobalUnitConversions.Add(new GlobalUnitConversion
                    {
                        BaseUnitId = it.BaseUnitId,
                        AltUnitId = it.AltUnitId,
                        Quantity = it.Quantity
                    });
                }
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = "Save failed: " + ex.Message });
            TempData["GucError"] = "Save failed: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }

        if (isAjax) return Json(new { success = true, message = "Unit conversions saved." });
        TempData["GucMessage"] = "Unit conversions saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<JsonResult> Delete(int id, CancellationToken ct = default)
    {
        try
        {
            var row = await _db.GlobalUnitConversions.FindAsync(new object?[] { id }, ct).ConfigureAwait(false);
            if (row == null)
                return Json(new { success = false, message = "Record not found" });
            _db.GlobalUnitConversions.Remove(row);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Json(new { success = true, message = "Row deleted." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
        }
    }
}
