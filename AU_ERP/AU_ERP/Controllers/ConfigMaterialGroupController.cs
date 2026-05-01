using AU_ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "AdminDepartment")]
public class ConfigMaterialGroupController : Controller
{
    private readonly AppDbContext _db;

    public ConfigMaterialGroupController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var list = await _db.MaterialGroups
            .AsNoTracking()
            .OrderBy(g => g.MaterialGroupCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(List<MaterialGroup>? materialGroups, CancellationToken cancellationToken = default)
    {
        var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);

        if (materialGroups == null || materialGroups.Count == 0)
        {
            if (isAjax) return Json(new { success = false, message = "No data received." });
            return RedirectToAction(nameof(Index));
        }

        try
        {
            foreach (var item in materialGroups)
            {
                if (string.IsNullOrWhiteSpace(item.MaterialGroupCode)) continue;
                item.MaterialGroupCode = item.MaterialGroupCode.Trim();
                if (string.IsNullOrEmpty(item.MaterialGroupCode)) continue;

                if (item.MaterialGroupCode.Length > 32)
                {
                    if (isAjax) return Json(new { success = false, message = "Code is limited to 32 characters." });
                    TempData["ConfigMgError"] = "Code is limited to 32 characters.";
                    return RedirectToAction(nameof(Index));
                }

                var title = (item.Description ?? "").Trim();
                if (string.IsNullOrEmpty(title) || title.Length > 200)
                {
                    if (isAjax) return Json(new { success = false, message = "Title is required (max 200 characters)." });
                    TempData["ConfigMgError"] = "Title is required (max 200 characters).";
                    return RedirectToAction(nameof(Index));
                }

                var existing = await _db.MaterialGroups.FindAsync(item.MaterialGroupCode, cancellationToken)
                    .ConfigureAwait(false);
                if (existing != null)
                {
                    existing.Description = title;
                }
                else
                {
                    item.Description = title;
                    item.AuthorizationGroup = null;
                    await _db.MaterialGroups.AddAsync(item, cancellationToken).ConfigureAwait(false);
                }
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (isAjax) return Json(new { success = true, message = "Material groups saved." });
            TempData["ConfigMgMessage"] = "Material groups saved.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = "Save failed: " + ex.Message });
            TempData["ConfigMgError"] = "Save failed: " + ex.Message;
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
            var item = await _db.MaterialGroups.FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (item == null)
                return Json(new { success = false, message = "Record not found." });

            var materialCount = await _db.CreateMaterialMaster
                .CountAsync(m => m.MaterialGroupCode == id, cancellationToken)
                .ConfigureAwait(false);
            if (materialCount > 0)
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot delete: {materialCount} material(s) use this group."
                });
            }

            _db.MaterialGroups.Remove(item);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Json(new { success = true, message = "Material group deleted." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
