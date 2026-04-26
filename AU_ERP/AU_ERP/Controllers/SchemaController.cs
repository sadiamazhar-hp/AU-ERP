using AU_ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "AdminDepartment")]
public class SchemaController : Controller
{
    private readonly AppDbContext _db;

    public SchemaController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var schemas = await _db.ConfigurationSchemas
            .AsNoTracking()
            .Include(s => s.SchemaCharges)
            .ThenInclude(x => x.Charge)
            .OrderBy(s => s.Title)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var charges = await _db.Charges.AsNoTracking()
            .OrderBy(c => c.Symbol)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        ViewBag.AllCharges = charges;
        return View(schemas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(List<ConfigurationSchemaFormRow>? items, CancellationToken ct = default)
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
                items[i].Title = (items[i].Title ?? "").Trim();
        }

        var toProcess = new List<ConfigurationSchemaFormRow>();
        foreach (var item in items)
        {
            if (item == null) continue;
            if (item.Id > 0)
            {
                toProcess.Add(item);
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Title))
                continue;

            toProcess.Add(item);
        }

        if (toProcess.Count == 0)
        {
            if (isAjax) return Json(new { success = false, message = "No rows to save." });
            return RedirectToAction(nameof(Index));
        }

        var validIds = new HashSet<int>(await _db.Charges.AsNoTracking().Select(c => c.Id).ToListAsync(ct).ConfigureAwait(false));

        foreach (var item in toProcess)
        {
            if (string.IsNullOrEmpty(item.Title) || item.Title.Length > 200)
            {
                if (isAjax) return Json(new { success = false, message = "Each schema needs a title (max 200 characters)." });
                TempData["SchemaError"] = "Each schema needs a title (max 200 characters).";
                return RedirectToAction(nameof(Index));
            }

            if (item.SelectedChargeIds != null)
            {
                foreach (var cid in item.SelectedChargeIds)
                {
                    if (!validIds.Contains(cid))
                    {
                        if (isAjax) return Json(new { success = false, message = "One or more selected charges are invalid. Refresh the page." });
                        TempData["SchemaError"] = "One or more selected charges are invalid.";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
        }

        var titles = toProcess.Select(s => s.Title).ToList();
        if (titles.Count != titles.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            if (isAjax) return Json(new { success = false, message = "Duplicate titles in the table." });
            TempData["SchemaError"] = "Duplicate titles in the table.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            foreach (var item in toProcess)
            {
                var taken = await _db.ConfigurationSchemas
                    .AsNoTracking()
                    .AnyAsync(s => s.Title == item.Title && s.Id != item.Id, ct)
                    .ConfigureAwait(false);
                if (taken)
                {
                    if (isAjax) return Json(new { success = false, message = $"Title \"{item.Title}\" is already used by another schema." });
                    TempData["SchemaError"] = $"Title \"{item.Title}\" is already used by another schema.";
                    return RedirectToAction(nameof(Index));
                }
            }

            foreach (var item in toProcess)
            {
                var ids = item.SelectedChargeIds?.Where(id => validIds.Contains(id)).Distinct().ToList() ?? new List<int>();

                if (item.Id > 0)
                {
                    var row = await _db.ConfigurationSchemas
                        .Include(s => s.SchemaCharges)
                        .FirstOrDefaultAsync(s => s.Id == item.Id, ct)
                        .ConfigureAwait(false);
                    if (row == null)
                    {
                        if (isAjax) return Json(new { success = false, message = "A schema was removed or not found. Refresh the page." });
                        TempData["SchemaError"] = "A schema was removed or not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    row.Title = item.Title!;
                    row.SchemaType = item.SchemaType == (int)ConfigurationSchemaType.Purchase
                        ? ConfigurationSchemaType.Purchase
                        : ConfigurationSchemaType.Sales;
                    _db.ConfigurationSchemaCharges.RemoveRange(row.SchemaCharges);
                    foreach (var cid in ids)
                        row.SchemaCharges.Add(new ConfigurationSchemaCharge { ChargeId = cid });
                }
                else
                {
                    var row = new ConfigurationSchema
                    {
                        Title = item.Title!,
                        SchemaType = item.SchemaType == (int)ConfigurationSchemaType.Purchase
                            ? ConfigurationSchemaType.Purchase
                            : ConfigurationSchemaType.Sales
                    };
                    foreach (var cid in ids)
                        row.SchemaCharges.Add(new ConfigurationSchemaCharge { ChargeId = cid });
                    _db.ConfigurationSchemas.Add(row);
                }
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = "Save failed: " + ex.Message });
            TempData["SchemaError"] = "Save failed: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }

        if (isAjax) return Json(new { success = true, message = "Schema saved." });
        TempData["SchemaMessage"] = "Schema saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<JsonResult> DeleteSchema(int id, CancellationToken ct = default)
    {
        try
        {
            var row = await _db.ConfigurationSchemas
                .Include(s => s.SchemaCharges)
                .FirstOrDefaultAsync(s => s.Id == id, ct)
                .ConfigureAwait(false);
            if (row == null)
                return Json(new { success = false, message = "Record not found" });

            _db.ConfigurationSchemas.Remove(row);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Json(new { success = true, message = "Schema deleted." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
        }
    }
}
