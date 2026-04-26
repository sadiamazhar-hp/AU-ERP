using AU_ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "AdminDepartment")]
public class ChargesController : Controller
{
    private readonly AppDbContext _db;

    public ChargesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var list = await _db.Charges.AsNoTracking()
            .OrderBy(c => c.Symbol)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(List<Charge>? items, CancellationToken ct = default)
    {
        var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);

        if (items == null || items.Count == 0)
        {
            if (isAjax) return Json(new { success = false, message = "No data received." });
            return RedirectToAction(nameof(Index));
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                items[i].Symbol = (items[i].Symbol ?? "").Trim();
                items[i].Description = (items[i].Description ?? "").Trim();
            }
        }

        var toProcess = new List<Charge>();
        foreach (var item in items)
        {
            if (item == null) continue;
            if (item.Id > 0)
            {
                toProcess.Add(item);
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Symbol) && string.IsNullOrWhiteSpace(item.Description))
                continue;

            toProcess.Add(item);
        }

        if (toProcess.Count == 0)
        {
            if (isAjax) return Json(new { success = false, message = "No rows to save." });
            return RedirectToAction(nameof(Index));
        }

        foreach (var item in toProcess)
        {
            if (string.IsNullOrEmpty(item.Symbol) || item.Symbol.Length > 32)
            {
                if (isAjax) return Json(new { success = false, message = "Each charge needs a symbol (max 32 characters)." });
                TempData["ChargeError"] = "Each charge needs a symbol (max 32 characters).";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(item.Description) || item.Description.Length > 500)
            {
                if (isAjax) return Json(new { success = false, message = "Description is required (max 500 characters)." });
                TempData["ChargeError"] = "Description is required (max 500 characters).";
                return RedirectToAction(nameof(Index));
            }

            if (item.ValueType != Charge.TypeNumeric && item.ValueType != Charge.TypePercentage)
            {
                if (isAjax) return Json(new { success = false, message = "Type must be Numeric or Percentage." });
                TempData["ChargeError"] = "Type must be Numeric or Percentage.";
                return RedirectToAction(nameof(Index));
            }

            if (item.Sign != Charge.SignAdd && item.Sign != Charge.SignSubtract)
            {
                if (isAjax) return Json(new { success = false, message = "Effect must be Add or Subtract." });
                TempData["ChargeError"] = "Effect must be Add or Subtract.";
                return RedirectToAction(nameof(Index));
            }
        }

        var symbols = toProcess.Select(c => c.Symbol).ToList();
        if (symbols.Count != symbols.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            if (isAjax) return Json(new { success = false, message = "Duplicate symbols in the table." });
            TempData["ChargeError"] = "Duplicate symbols in the table.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            foreach (var item in toProcess)
            {
                var taken = await _db.Charges
                    .AnyAsync(c => c.Symbol == item.Symbol && c.Id != item.Id, ct)
                    .ConfigureAwait(false);
                if (taken)
                {
                    if (isAjax) return Json(new { success = false, message = $"Symbol \"{item.Symbol}\" is already used by another charge." });
                    TempData["ChargeError"] = $"Symbol \"{item.Symbol}\" is already used by another charge.";
                    return RedirectToAction(nameof(Index));
                }
            }

            foreach (var item in toProcess)
            {
                if (item.Id > 0)
                {
                    var row = await _db.Charges.FirstOrDefaultAsync(c => c.Id == item.Id, ct).ConfigureAwait(false);
                    if (row == null)
                    {
                        if (isAjax) return Json(new { success = false, message = "A charge was removed or not found. Refresh the page." });
                        TempData["ChargeError"] = "A charge was removed or not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    row.Symbol = item.Symbol;
                    row.Description = item.Description;
                    row.ValueType = item.ValueType;
                    row.Sign = item.Sign;
                }
                else
                {
                    _db.Charges.Add(new Charge
                    {
                        Symbol = item.Symbol,
                        Description = item.Description,
                        ValueType = item.ValueType,
                        Sign = item.Sign
                    });
                }
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = "Save failed: " + ex.Message });
            TempData["ChargeError"] = "Save failed: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }

        if (isAjax) return Json(new { success = true, message = "Charges saved." });
        TempData["ChargeMessage"] = "Charges saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<JsonResult> DeleteCharge(int id, CancellationToken ct = default)
    {
        try
        {
            var row = await _db.Charges.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
            if (row == null)
                return Json(new { success = false, message = "Record not found" });

            _db.Charges.Remove(row);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Json(new { success = true, message = "Charge deleted." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
        }
    }
}
