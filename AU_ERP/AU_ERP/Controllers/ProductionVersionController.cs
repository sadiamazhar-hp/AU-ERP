using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;
using AU_ERP.Services;
using AU_ERP.Validation;

namespace AU_ERP.Controllers
{
    [Authorize(Policy = "AdminDepartment")]
    public class ProductionVersionController : Controller
    {
        private readonly AppDbContext _db;

        public ProductionVersionController(AppDbContext db) => _db = db;

        private async Task PrepareViewBags(CancellationToken ct = default)
        {
            ViewBag.Plants = new SelectList(
                await _db.PlantsSamples.AsNoTracking().OrderBy(p => p.PlantID).ToListAsync(ct),
                "PlantID", "PlantName");

            ViewBag.Boms = await _db.BomHeadersSamples.AsNoTracking()
                .ActiveMaster()
                .OrderBy(b => b.BomID)
                .Select(b => new { b.BomID, Display = b.BOMCode + " - " + b.BOMTitle })
                .ToListAsync(ct);

            ViewBag.Routings = await _db.RoutingHeadersSamples.AsNoTracking()
                .OrderBy(r => r.RoutingID)
                .Select(r => new { r.RoutingID, Display = r.RoutingID + " - " + (r.Title ?? "") })
                .ToListAsync(ct);
        }

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            await PrepareViewBags(ct);

            var list = await _db.ProductionVersions
                .AsNoTracking()
                .Include(p => p.Plant)
                .Include(p => p.BomHeader)
                .Include(p => p.RoutingHeader)
                .OrderByDescending(p => p.Id)
                .ToListAsync(ct);

            return View(list);
        }

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] ProdVersionDto dto, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.PlantId))
                    return Json(new { success = false, message = "Plant is required." });
                if (string.IsNullOrWhiteSpace(dto.Version))
                    return Json(new { success = false, message = "Version is required." });

                var pv = new ProductionVersion
                {
                    PlantId = dto.PlantId,
                    Version = dto.Version?.Trim(),
                    ValidFrom = dto.ValidFrom ?? DateTime.Now,
                    BomId = dto.BomId,
                    RoutingId = dto.RoutingId
                };

                await _db.ProductionVersions.AddAsync(pv, ct);
                await _db.SaveChangesAsync(ct);

                return Json(new { success = true, message = "Production Version Created Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Save failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetForEdit(int id, CancellationToken ct = default)
        {
            var pv = await _db.ProductionVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (pv == null)
                return Json(new { success = false, message = "Production Version not found." });

            return Json(new
            {
                success = true,
                data = new
                {
                    pv.Id,
                    pv.PlantId,
                    pv.Version,
                    ValidFrom = pv.ValidFrom?.ToString("yyyy-MM-dd"),
                    pv.BomId,
                    pv.RoutingId
                }
            });
        }

        [HttpPost]
        public async Task<JsonResult> Update([FromBody] ProdVersionDto dto, CancellationToken ct = default)
        {
            try
            {
                var pv = await _db.ProductionVersions.FindAsync(new object[] { dto.Id }, ct);
                if (pv == null)
                    return Json(new { success = false, message = "Production Version not found." });

                if (string.IsNullOrWhiteSpace(dto.Version))
                    return Json(new { success = false, message = "Version is required." });

                pv.PlantId = dto.PlantId;
                pv.Version = dto.Version?.Trim();
                pv.ValidFrom = dto.ValidFrom ?? pv.ValidFrom;
                pv.BomId = dto.BomId;
                pv.RoutingId = dto.RoutingId;

                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "Production Version Updated Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Update failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Delete(int id, CancellationToken ct = default)
        {
            try
            {
                var pv = await _db.ProductionVersions.FindAsync(new object[] { id }, ct);
                if (pv == null)
                    return Json(new { success = false, message = "Production Version not found." });

                _db.ProductionVersions.Remove(pv);
                await _db.SaveChangesAsync(ct);

                return Json(new { success = true, message = "Production Version Deleted Successfully !" });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "production version") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "production version") });
            }
        }
    }

    public class ProdVersionDto
    {
        public int Id { get; set; }
        public string? PlantId { get; set; }
        public string? Version { get; set; }
        public DateTime? ValidFrom { get; set; }
        public int? BomId { get; set; }
        public int? RoutingId { get; set; }
    }
}
