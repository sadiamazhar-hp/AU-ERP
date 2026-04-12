using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;

namespace AU_ERP.Controllers
{
    public class BOMController : Controller
    {
        private readonly AppDbContext _db;

        public BOMController(AppDbContext db) => _db = db;

        private async Task PrepareViewBags(CancellationToken ct = default)
        {
            ViewBag.BomLevels = new SelectList(
                await _db.BOMLevelsSamples.AsNoTracking().OrderBy(l => l.LevelID).ToListAsync(ct),
                "LevelID", "LevelName");

            ViewBag.Plants = new SelectList(
                await _db.PlantsSamples.AsNoTracking().OrderBy(p => p.PlantID).ToListAsync(ct),
                "PlantID", "PlantName");

            ViewBag.MaterialList = await _db.CreateMaterialMaster.AsNoTracking()
                .OrderBy(m => m.MaterialNumber).ToListAsync(ct);
        }

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            await PrepareViewBags(ct);

            var list = await _db.BomHeadersSamples
                .AsNoTracking()
                .Include(h => h.BOMLevel)
                .Include(h => h.PlantSample)
                .Include(h => h.BomItemsSamples)
                .OrderByDescending(h => h.BomID)
                .ToListAsync(ct);

            return View(list);
        }

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] BomCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                var code = dto.BOMCode?.Trim();
                if (string.IsNullOrEmpty(code))
                    return Json(new { success = false, message = "BOM Code is required." });

                if (code.Length > 5)
                    return Json(new { success = false, message = "BOM Code must be 5 characters or less." });

                var header = new BomHeadersSample
                {
                    BOMCode = code,
                    BOMTitle = dto.BOMTitle?.Trim(),
                    BLevel = dto.BLevel,
                    Plant = dto.Plant,
                    BaseQty = dto.BaseQty,
                    ValidFrom = dto.ValidFrom
                };

                if (dto.Items != null)
                {
                    foreach (var item in dto.Items)
                    {
                        var compNum = item.MaterialNumber?.Trim();
                        if (!string.IsNullOrEmpty(compNum))
                        {
                            var compExists = await _db.CreateMaterialMaster
                                .AsNoTracking()
                                .AnyAsync(m => m.MaterialNumber == compNum, ct);

                            if (!compExists)
                                return Json(new { success = false, message = $"Component material '{compNum}' does not exist." });
                        }

                        header.BomItemsSamples.Add(new BomItemsSample
                        {
                            MaterialNumber = compNum,
                            Quantity = item.Quantity,
                            UoM = item.UoM,
                            ScrapPercentage = item.ScrapPercentage
                        });
                    }
                }

                await _db.BomHeadersSamples.AddAsync(header, ct);
                await _db.SaveChangesAsync(ct);

                return Json(new { success = true, message = "BOM Created Successfully !", bomId = header.BomID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Save failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetBomForEdit(int id, CancellationToken ct = default)
        {
            var bom = await _db.BomHeadersSamples
                .AsNoTracking()
                .Include(h => h.BomItemsSamples)
                .FirstOrDefaultAsync(h => h.BomID == id, ct);

            if (bom == null)
                return Json(new { success = false, message = "BOM not found." });

            return Json(new
            {
                success = true,
                bom = new
                {
                    bom.BomID,
                    bom.BOMCode,
                    bom.BOMTitle,
                    bom.BLevel,
                    bom.Plant,
                    bom.BaseQty,
                    ValidFrom = bom.ValidFrom?.ToString("yyyy-MM-dd"),
                    Items = bom.BomItemsSamples.Select(i => new
                    {
                        i.ItemID,
                        i.MaterialNumber,
                        i.Quantity,
                        i.UoM,
                        i.ScrapPercentage
                    })
                }
            });
        }

        [HttpPost]
        public async Task<JsonResult> Update([FromBody] BomCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                var header = await _db.BomHeadersSamples
                    .Include(h => h.BomItemsSamples)
                    .FirstOrDefaultAsync(h => h.BomID == dto.BomID, ct);

                if (header == null)
                    return Json(new { success = false, message = "BOM not found." });

                var code = dto.BOMCode?.Trim();
                if (string.IsNullOrEmpty(code))
                    return Json(new { success = false, message = "BOM Code is required." });

                if (code.Length > 5)
                    return Json(new { success = false, message = "BOM Code must be 5 characters or less." });

                header.BOMCode = code;
                header.BOMTitle = dto.BOMTitle?.Trim();
                header.BLevel = dto.BLevel;
                header.Plant = dto.Plant;
                header.BaseQty = dto.BaseQty;
                header.ValidFrom = dto.ValidFrom;

                _db.BomItemsSamples.RemoveRange(header.BomItemsSamples);

                if (dto.Items != null)
                {
                    foreach (var item in dto.Items)
                    {
                        var compNum = item.MaterialNumber?.Trim();
                        if (!string.IsNullOrEmpty(compNum))
                        {
                            var compExists = await _db.CreateMaterialMaster
                                .AsNoTracking()
                                .AnyAsync(m => m.MaterialNumber == compNum, ct);

                            if (!compExists)
                                return Json(new { success = false, message = $"Component material '{compNum}' does not exist." });
                        }

                        header.BomItemsSamples.Add(new BomItemsSample
                        {
                            MaterialNumber = compNum,
                            Quantity = item.Quantity,
                            UoM = item.UoM,
                            ScrapPercentage = item.ScrapPercentage
                        });
                    }
                }

                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "BOM Updated Successfully !" });
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
                var header = await _db.BomHeadersSamples
                    .Include(h => h.BomItemsSamples)
                    .FirstOrDefaultAsync(h => h.BomID == id, ct);

                if (header == null)
                    return Json(new { success = false, message = "BOM not found." });

                _db.BomItemsSamples.RemoveRange(header.BomItemsSamples);
                _db.BomHeadersSamples.Remove(header);
                await _db.SaveChangesAsync(ct);

                return Json(new { success = true, message = "BOM Deleted Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }

    public class BomCreateDto
    {
        public int BomID { get; set; }
        public string? BOMCode { get; set; }
        public string? BOMTitle { get; set; }
        public int? BLevel { get; set; }
        public string? Plant { get; set; }
        public decimal? BaseQty { get; set; }
        public DateTime? ValidFrom { get; set; }
        public List<BomItemDto>? Items { get; set; }
    }

    public class BomItemDto
    {
        public string? MaterialNumber { get; set; }
        public decimal? Quantity { get; set; }
        public string? UoM { get; set; }
        public decimal? ScrapPercentage { get; set; }
    }
}
