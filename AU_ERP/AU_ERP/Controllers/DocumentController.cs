using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Configuration;
using AU_ERP.Models;
using AU_ERP.Services;

namespace AU_ERP.Controllers
{
    [Authorize(Policy = "AdminDepartment")]
    public class DocumentController : Controller
    {
        private readonly AppDbContext _context;

        public DocumentController(AppDbContext context) => _context = context;

        // ──── Document Types ────

        [HttpGet]
        public async Task<IActionResult> DocumentTypes()
        {
            var list = await _context.DocumentTypes.ToListAsync();
            return View("DocumentTypes", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentTypes(List<DocumentType>? documentTypes)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (documentTypes == null || documentTypes.Count == 0)
            {
                if (isAjax) return Json(new { success = false, message = "No data received." });
                return RedirectToAction(nameof(DocumentTypes));
            }

            try
            {
                foreach (var item in documentTypes)
                {
                    if (item.DocumentTypeID > 0)
                    {
                        var existing = await _context.DocumentTypes.FindAsync(item.DocumentTypeID);
                        if (existing != null)
                        {
                            existing.DocCode = item.DocCode;
                            existing.Description = item.Description;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(item.Description)) continue;
                        await _context.DocumentTypes.AddAsync(item);
                    }
                }

                await _context.SaveChangesAsync();
                if (isAjax) return Json(new { success = true, message = "Document Types Saved Successfully !" });
                return RedirectToAction(nameof(DocumentTypes));
            }
            catch (Exception ex)
            {
                if (isAjax) return Json(new { success = false, message = "Save failed: " + ex.Message });
                ModelState.AddModelError("", "Save failed: " + ex.Message);
                return View("DocumentTypes", documentTypes);
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteDocumentType(int id)
        {
            try
            {
                var item = await _context.DocumentTypes.FindAsync(id);
                if (item == null)
                    return Json(new { success = false, message = "Record not found" });

                _context.DocumentTypes.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Document Type Deleted Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // ──── Document Ranges ────

        [HttpGet]
        public async Task<IActionResult> DocumentRanges()
        {
            ViewBag.DocumentTypes = await _context.DocumentTypes
                .Select(d => new SelectListItem
                {
                    Value = d.DocumentTypeID.ToString(),
                    Text = (d.DocCode ?? "") + " - " + d.Description
                }).ToListAsync();

            var data = await _context.DocumentRanges
                .Include(r => r.DocumentType)
                .ToListAsync();
            return View("DocumentRanges", data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentRanges(List<DocumentRange> ranges)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (ranges == null || ranges.Count == 0)
            {
                if (isAjax) return Json(new { success = false, message = "No data received." });
                return RedirectToAction("DocumentRanges");
            }

            try
            {
                var existingRows = await _context.DocumentRanges.AsNoTracking().ToListAsync();
                var effective = existingRows.ToDictionary(x => x.RangeID, x => (x.FromNumber, x.ToNumber));
                var tempId = -1;
                foreach (var item in ranges)
                {
                    if (item.DocumentTypeID == null || item.DocumentTypeID == 0)
                        continue;

                    if (!NumberRangeMaintenance.TryValidateDocumentRange(item.FromNumber, item.ToNumber, out var rangeError))
                    {
                        var msg = rangeError ?? "Invalid document range.";
                        if (isAjax) return Json(new { success = false, message = msg });
                        TempData["Error"] = msg;
                        return RedirectToAction("DocumentRanges");
                    }

                    var key = item.RangeID > 0 ? item.RangeID : tempId--;
                    effective[key] = (item.FromNumber, item.ToNumber);
                }

                var bounded = effective
                    .Where(x => x.Value.FromNumber.HasValue && x.Value.ToNumber.HasValue)
                    .Select(x => (x.Key, From: x.Value.FromNumber!.Value, To: x.Value.ToNumber!.Value))
                    .ToList();
                for (var i = 0; i < bounded.Count; i++)
                {
                    for (var j = i + 1; j < bounded.Count; j++)
                    {
                        var a = bounded[i];
                        var b = bounded[j];
                        if (!NumberRangeMaintenance.RangesOverlap(a.From, a.To, b.From, b.To))
                            continue;

                        var msg = $"Document ranges conflict: {a.From}-{a.To} overlaps {b.From}-{b.To}.";
                        if (isAjax) return Json(new { success = false, message = msg });
                        TempData["Error"] = msg;
                        return RedirectToAction("DocumentRanges");
                    }
                }

                foreach (var item in ranges)
                {
                    if (item.DocumentTypeID == null || item.DocumentTypeID == 0) continue;

                    var normalizedCurrent = NumberRangeMaintenance.NormalizeDocumentLastIssued(
                        item.FromNumber, item.ToNumber, item.CurrentNumber);

                    if (item.RangeID > 0)
                    {
                        var existing = await _context.DocumentRanges.FindAsync(item.RangeID);
                        if (existing != null)
                        {
                            existing.DocumentTypeID = item.DocumentTypeID;
                            existing.FromNumber = item.FromNumber;
                            existing.ToNumber = item.ToNumber;
                            existing.CurrentNumber = normalizedCurrent;
                        }
                    }
                    else
                    {
                        item.CurrentNumber = normalizedCurrent;
                        await _context.DocumentRanges.AddAsync(item);
                    }
                }

                await _context.SaveChangesAsync();
                if (isAjax) return Json(new { success = true, message = "Document Ranges Saved Successfully !" });
                TempData["Success"] = "Data Saved Successfully!";
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (isAjax) return Json(new { success = false, message = "Save failed: " + msg });
                TempData["Error"] = $"Save failed: {msg}";
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (isAjax) return Json(new { success = false, message = "Save failed: " + msg });
                TempData["Error"] = "Save failed: " + msg;
            }

            return RedirectToAction("DocumentRanges");
        }

        [HttpPost]
        public async Task<JsonResult> DeleteDocumentRange(int id)
        {
            try
            {
                var item = await _context.DocumentRanges.FindAsync(id);
                if (item == null)
                    return Json(new { success = false, message = "Record not found" });

                _context.DocumentRanges.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Document Range Deleted Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // ──── Document Integration ────

        [HttpGet]
        public async Task<IActionResult> Integration(CancellationToken ct = default)
        {
            ViewBag.DocumentTypes = await _context.DocumentTypes
                .Select(d => new SelectListItem
                {
                    Value = d.DocumentTypeID.ToString(),
                    Text = (d.DocCode ?? "") + " - " + d.Description
                }).ToListAsync(ct)
                .ConfigureAwait(false);

            ViewBag.ModuleKeys = ModuleKeys.All
                .Select(x => new SelectListItem { Value = x.Key, Text = x.Display })
                .ToList();

            var data = await _context.DocumentIntegrations
                .Include(i => i.DocumentType)
                .OrderBy(i => i.ModuleKey)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            ViewBag.IntegrationWarnings = await DocumentIntegrationDiagnostics.GetActiveIntegrationWarningsAsync(_context, ct)
                .ConfigureAwait(false);
            return View("Integration", data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Integration([FromForm] List<DocumentIntegration>? integrations, CancellationToken ct = default)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            var rows = (integrations ?? new List<DocumentIntegration>())
                .Where(r => !string.IsNullOrWhiteSpace(r.ModuleKey) && r.DocumentTypeID > 0)
                .ToList();

            // Never wipe the integration table because some rows posted empty module/document or the binder failed silently.
            if (rows.Count == 0 &&
                Request.HasFormContentType &&
                Request.Form.Keys.Any(k => k.StartsWith("integrations[", StringComparison.OrdinalIgnoreCase)))
            {
                const string guarded =
                    "No valid integration rows were saved. Ensure every row has both a Module and Document selected.";
                if (isAjax)
                    return Json(new { success = false, message = guarded });
                TempData["Error"] = guarded;
                return RedirectToAction(nameof(Integration));
            }

            // Clear-all is only allowed when nothing was posted under the integrations[*] prefix.
            if (rows.Count == 0)
            {
                try
                {
                    var removeAll = await _context.DocumentIntegrations.ToListAsync();
                    _context.DocumentIntegrations.RemoveRange(removeAll);
                    await _context.SaveChangesAsync();
                    if (isAjax) return Json(new { success = true, message = "Integration mappings cleared." });
                    TempData["Success"] = "Integration mappings cleared.";
                    return RedirectToAction(nameof(Integration));
                }
                catch (Exception ex)
                {
                    var msg = ex.InnerException?.Message ?? ex.Message;
                    if (isAjax) return Json(new { success = false, message = "Save failed: " + msg });
                    TempData["Error"] = msg;
                    return RedirectToAction(nameof(Integration));
                }
            }

            var dupKeys = rows.GroupBy(r => r.ModuleKey.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (dupKeys.Count > 0)
            {
                var msg = "Each module can only appear once: duplicate " + string.Join(", ", dupKeys) + ".";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Integration));
            }

            foreach (var r in rows)
            {
                var hasRange = await _context.DocumentRanges.AsNoTracking()
                    .AnyAsync(x => x.DocumentTypeID == r.DocumentTypeID
                        && x.FromNumber.HasValue && x.ToNumber.HasValue
                        && (x.CurrentNumber ?? (x.FromNumber!.Value - 1)) < x.ToNumber!.Value);
                if (!hasRange)
                {
                    var msg = $"Document type for module '{r.ModuleKey}' has no available number range (configure ranges with remaining capacity).";
                    if (isAjax) return Json(new { success = false, message = msg });
                    TempData["Error"] = msg;
                    return RedirectToAction(nameof(Integration));
                }
            }

            try
            {
                var postedIds = rows.Where(r => r.DocumentIntegrationID > 0).Select(r => r.DocumentIntegrationID).ToHashSet();
                var toRemove = await _context.DocumentIntegrations
                    .Where(i => !postedIds.Contains(i.DocumentIntegrationID))
                    .ToListAsync();
                _context.DocumentIntegrations.RemoveRange(toRemove);

                var utc = DateTime.UtcNow;
                foreach (var r in rows)
                {
                    r.ModuleKey = r.ModuleKey.Trim();
                    if (r.DocumentIntegrationID > 0)
                    {
                        var existing = await _context.DocumentIntegrations.FindAsync(r.DocumentIntegrationID);
                        if (existing != null)
                        {
                            existing.ModuleKey = r.ModuleKey;
                            existing.DocumentTypeID = r.DocumentTypeID;
                            existing.IsActive = r.IsActive;
                            existing.UpdatedAt = utc;
                        }
                    }
                    else
                    {
                        r.CreatedAt = utc;
                        r.UpdatedAt = null;
                        await _context.DocumentIntegrations.AddAsync(r);
                    }
                }

                await _context.SaveChangesAsync();
                if (isAjax) return Json(new { success = true, message = "Document integration saved successfully." });
                TempData["Success"] = "Data saved successfully!";
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (isAjax) return Json(new { success = false, message = "Save failed: " + msg });
                TempData["Error"] = $"Save failed: {msg}";
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (isAjax) return Json(new { success = false, message = "Save failed: " + msg });
                TempData["Error"] = "Save failed: " + msg;
            }

            return RedirectToAction(nameof(Integration));
        }

        [HttpPost]
        public async Task<JsonResult> DeleteDocumentIntegration(int id)
        {
            try
            {
                var item = await _context.DocumentIntegrations.FindAsync(id);
                if (item == null)
                    return Json(new { success = false, message = "Record not found" });

                _context.DocumentIntegrations.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Integration row deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}
