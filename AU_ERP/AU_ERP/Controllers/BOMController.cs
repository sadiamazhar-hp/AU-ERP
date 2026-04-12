using System.Globalization;
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

        /// <summary>Next numeric BOM code; first issued code is 100000.</summary>
        private async Task<string> AllocateNextBomCodeAsync(CancellationToken ct = default)
        {
            const long floor = 100000;
            var codes = await _db.BomHeadersSamples.AsNoTracking()
                .Where(h => h.BOMCode != null)
                .Select(h => h.BOMCode!)
                .ToListAsync(ct);

            var best = floor - 1;
            foreach (var c in codes)
            {
                if (long.TryParse(c.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var v) && v > best)
                    best = v;
            }

            return (best + 1).ToString(CultureInfo.InvariantCulture);
        }

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

            ViewBag.UomList = await _db.UnitOfMeasurements.AsNoTracking()
                .OrderBy(u => u.Code)
                .ToListAsync(ct);
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
                    code = await AllocateNextBomCodeAsync(ct);

                if (code.Length > 20)
                    return Json(new { success = false, message = "BOM Code must be 20 characters or less." });

                if (await _db.BomHeadersSamples.AnyAsync(h => h.BOMCode == code, ct))
                    return Json(new { success = false, message = "This BOM code is already in use." });

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

                            if (item.UomId is null || item.UomId <= 0)
                                return Json(new { success = false, message = "Each component line must have a UOM selected." });

                            var uomOk = await _db.UnitOfMeasurements.AsNoTracking()
                                .AnyAsync(u => u.Id == item.UomId.Value, ct);
                            if (!uomOk)
                                return Json(new { success = false, message = "One or more component UOM values are invalid." });
                        }

                        header.BomItemsSamples.Add(new BomItemsSample
                        {
                            MaterialNumber = compNum,
                            Quantity = item.Quantity,
                            UomId = string.IsNullOrEmpty(compNum) ? null : item.UomId,
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
                    .ThenInclude(i => i.Uom)
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
                        i.UomId,
                        UomCode = i.Uom != null ? i.Uom.Code : null,
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
                    code = await AllocateNextBomCodeAsync(ct);

                if (code.Length > 20)
                    return Json(new { success = false, message = "BOM Code must be 20 characters or less." });

                if (await _db.BomHeadersSamples.AnyAsync(h => h.BomID != dto.BomID && h.BOMCode == code, ct))
                    return Json(new { success = false, message = "This BOM code is already in use." });

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

                            if (item.UomId is null || item.UomId <= 0)
                                return Json(new { success = false, message = "Each component line must have a UOM selected." });

                            var uomOk = await _db.UnitOfMeasurements.AsNoTracking()
                                .AnyAsync(u => u.Id == item.UomId.Value, ct);
                            if (!uomOk)
                                return Json(new { success = false, message = "One or more component UOM values are invalid." });
                        }

                        header.BomItemsSamples.Add(new BomItemsSample
                        {
                            MaterialNumber = compNum,
                            Quantity = item.Quantity,
                            UomId = string.IsNullOrEmpty(compNum) ? null : item.UomId,
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
        public int? UomId { get; set; }
        public decimal? ScrapPercentage { get; set; }
    }
}
