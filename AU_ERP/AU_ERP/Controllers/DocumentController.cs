using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
    }
}
