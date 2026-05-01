using AU_ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "AdminDepartment")]
public class ConfigPlantController : Controller
{
    private readonly AppDbContext _db;

    public ConfigPlantController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var list = await _db.PlantsSamples
            .AsNoTracking()
            .OrderBy(p => p.PlantID)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(List<PlantsSample>? plants, CancellationToken cancellationToken = default)
    {
        var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);

        if (plants == null || plants.Count == 0)
        {
            if (isAjax) return Json(new { success = false, message = "No data received." });
            return RedirectToAction(nameof(Index));
        }

        try
        {
            foreach (var item in plants)
            {
                if (string.IsNullOrWhiteSpace(item.PlantID)) continue;
                item.PlantID = item.PlantID.Trim();
                if (string.IsNullOrEmpty(item.PlantID)) continue;

                if (item.PlantID.Length > 32)
                {
                    if (isAjax) return Json(new { success = false, message = "Code is limited to 32 characters." });
                    TempData["ConfigPlantError"] = "Code is limited to 32 characters.";
                    return RedirectToAction(nameof(Index));
                }

                var title = (item.PlantName ?? "").Trim();
                if (string.IsNullOrEmpty(title) || title.Length > 200)
                {
                    if (isAjax) return Json(new { success = false, message = "Title is required (max 200 characters)." });
                    TempData["ConfigPlantError"] = "Title is required (max 200 characters).";
                    return RedirectToAction(nameof(Index));
                }

                var existing = await _db.PlantsSamples.FindAsync(item.PlantID, cancellationToken)
                    .ConfigureAwait(false);
                if (existing != null)
                {
                    existing.PlantName = title;
                }
                else
                {
                    item.PlantName = title;
                    await _db.PlantsSamples.AddAsync(item, cancellationToken).ConfigureAwait(false);
                }
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (isAjax) return Json(new { success = true, message = "Plants saved." });
            TempData["ConfigPlantMessage"] = "Plants saved.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = "Save failed: " + ex.Message });
            TempData["ConfigPlantError"] = "Save failed: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<JsonResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        id = (id ?? string.Empty).Trim();
        try
        {
            var item = await _db.PlantsSamples.FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (item == null)
                return Json(new { success = false, message = "Record not found." });

            if (await _db.CreateMaterialMaster.AnyAsync(m => m.DeliveringPlantCode == id, cancellationToken)
                    .ConfigureAwait(false))
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot delete: one or more materials use this plant as delivering plant."
                });
            }

            if (await _db.BomHeadersSamples.AnyAsync(b => b.Plant == id, cancellationToken).ConfigureAwait(false))
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot delete: one or more BOM headers reference this plant."
                });
            }

            if (await _db.WorkCenterMasterSamples.AnyAsync(w => w.PlantID == id, cancellationToken)
                    .ConfigureAwait(false))
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot delete: one or more work centres are assigned to this plant."
                });
            }

            if (await _db.RoutingHeadersSamples.AnyAsync(r => r.PlantID == id, cancellationToken)
                    .ConfigureAwait(false))
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot delete: one or more routings are assigned to this plant."
                });
            }

            if (await _db.ProductionVersions.AnyAsync(p => p.PlantId == id, cancellationToken).ConfigureAwait(false))
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot delete: one or more product versions use this plant."
                });
            }

            if (await _db.SalesQuotations.AnyAsync(s => s.PlantId == id, cancellationToken).ConfigureAwait(false))
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot delete: one or more sales quotations use this plant."
                });
            }

            if (await _db.SalesOrders.AnyAsync(s => s.PlantId == id, cancellationToken).ConfigureAwait(false))
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot delete: one or more sales orders use this plant."
                });
            }

            _db.PlantsSamples.Remove(item);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Json(new { success = true, message = "Plant deleted." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
