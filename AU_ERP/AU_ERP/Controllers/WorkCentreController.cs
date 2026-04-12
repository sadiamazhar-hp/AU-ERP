using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;

namespace AU_ERP.Controllers
{
    public class WorkCentreController : Controller
    {
        private readonly AppDbContext _db;

        public WorkCentreController(AppDbContext db) => _db = db;

        private async Task PrepareViewBags(CancellationToken ct = default)
        {
            ViewBag.Plants = new SelectList(
                await _db.PlantsSamples.AsNoTracking().OrderBy(p => p.PlantID).ToListAsync(ct),
                "PlantID", "PlantName");
        }

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            await PrepareViewBags(ct);

            var list = await _db.WorkCenterMasterSamples
                .AsNoTracking()
                .Include(w => w.Plant)
                .OrderByDescending(w => w.ID)
                .ToListAsync(ct);

            return View(list);
        }

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] WorkCentreDto dto, CancellationToken ct = default)
        {
            try
            {
                var name = dto.WorkCenterName?.Trim();
                if (string.IsNullOrEmpty(name))
                    return Json(new { success = false, message = "Work Centre Name is required." });

                var wc = new WorkCenterMasterSample
                {
                    WorkCenterName = name,
                    PlantID = dto.PlantID,
                    Description = dto.Description?.Trim(),
                    AvailableCapacity = dto.AvailableCapacity,
                    UtilizationPercentage = dto.UtilizationPercentage,
                    SetupTime = dto.SetupTime,
                    SetupUOM = dto.SetupUOM?.Trim(),
                    MachineTime = dto.MachineTime,
                    MachineUOM = dto.MachineUOM?.Trim(),
                    LaborTime = dto.LaborTime,
                    LaborUOM = dto.LaborUOM?.Trim(),
                    CreatedAt = DateTime.Now
                };

                await _db.WorkCenterMasterSamples.AddAsync(wc, ct);
                await _db.SaveChangesAsync(ct);

                return Json(new { success = true, message = "Work Centre Created Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Save failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetForEdit(int id, CancellationToken ct = default)
        {
            var wc = await _db.WorkCenterMasterSamples
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.ID == id, ct);

            if (wc == null)
                return Json(new { success = false, message = "Work Centre not found." });

            return Json(new
            {
                success = true,
                data = new
                {
                    wc.ID,
                    wc.WorkCenterName,
                    wc.PlantID,
                    wc.Description,
                    wc.AvailableCapacity,
                    wc.UtilizationPercentage,
                    wc.SetupTime,
                    wc.SetupUOM,
                    wc.MachineTime,
                    wc.MachineUOM,
                    wc.LaborTime,
                    wc.LaborUOM
                }
            });
        }

        [HttpPost]
        public async Task<JsonResult> Update([FromBody] WorkCentreDto dto, CancellationToken ct = default)
        {
            try
            {
                var wc = await _db.WorkCenterMasterSamples
                    .FirstOrDefaultAsync(w => w.ID == dto.ID, ct);

                if (wc == null)
                    return Json(new { success = false, message = "Work Centre not found." });

                var name = dto.WorkCenterName?.Trim();
                if (string.IsNullOrEmpty(name))
                    return Json(new { success = false, message = "Work Centre Name is required." });

                wc.WorkCenterName = name;
                wc.PlantID = dto.PlantID;
                wc.Description = dto.Description?.Trim();
                wc.AvailableCapacity = dto.AvailableCapacity;
                wc.UtilizationPercentage = dto.UtilizationPercentage;
                wc.SetupTime = dto.SetupTime;
                wc.SetupUOM = dto.SetupUOM?.Trim();
                wc.MachineTime = dto.MachineTime;
                wc.MachineUOM = dto.MachineUOM?.Trim();
                wc.LaborTime = dto.LaborTime;
                wc.LaborUOM = dto.LaborUOM?.Trim();

                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "Work Centre Updated Successfully !" });
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
                var wc = await _db.WorkCenterMasterSamples
                    .FirstOrDefaultAsync(w => w.ID == id, ct);

                if (wc == null)
                    return Json(new { success = false, message = "Work Centre not found." });

                _db.WorkCenterMasterSamples.Remove(wc);
                await _db.SaveChangesAsync(ct);

                return Json(new { success = true, message = "Work Centre Deleted Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }

    public class WorkCentreDto
    {
        public int ID { get; set; }
        public string? WorkCenterName { get; set; }
        public string? PlantID { get; set; }
        public string? Description { get; set; }
        public int? AvailableCapacity { get; set; }
        public int? UtilizationPercentage { get; set; }
        public decimal? SetupTime { get; set; }
        public string? SetupUOM { get; set; }
        public decimal? MachineTime { get; set; }
        public string? MachineUOM { get; set; }
        public decimal? LaborTime { get; set; }
        public string? LaborUOM { get; set; }
    }
}
