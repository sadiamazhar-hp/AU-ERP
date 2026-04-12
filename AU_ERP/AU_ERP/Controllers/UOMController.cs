using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;

namespace AU_ERP.Controllers
{
    public class UOMController : Controller
    {
        private readonly AppDbContext _context;

        public UOMController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _context.UnitOfMeasurements.OrderBy(u => u.Code).ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(List<UnitOfMeasurement>? items)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (items == null || items.Count == 0)
            {
                if (isAjax) return Json(new { success = false, message = "No data received." });
                return RedirectToAction(nameof(Index));
            }

            try
            {
                foreach (var item in items)
                {
                    if (item.Id > 0)
                    {
                        var existing = await _context.UnitOfMeasurements.FindAsync(item.Id);
                        if (existing != null)
                        {
                            existing.Code = item.Code?.Trim();
                            existing.Description = item.Description?.Trim();
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(item.Code) || string.IsNullOrWhiteSpace(item.Description))
                            continue;
                        await _context.UnitOfMeasurements.AddAsync(new UnitOfMeasurement
                        {
                            Code = item.Code?.Trim(),
                            Description = item.Description?.Trim()
                        });
                    }
                }

                await _context.SaveChangesAsync();
                if (isAjax) return Json(new { success = true, message = "UOM Saved Successfully !" });
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (isAjax) return Json(new { success = false, message = "Save failed: " + ex.Message });
                ModelState.AddModelError("", "Save failed: " + ex.Message);
                return View(items);
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteUOM(int id)
        {
            try
            {
                var row = await _context.UnitOfMeasurements.FindAsync(id);
                if (row == null)
                    return Json(new { success = false, message = "Record not found" });

                _context.UnitOfMeasurements.Remove(row);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "UOM Deleted Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}
