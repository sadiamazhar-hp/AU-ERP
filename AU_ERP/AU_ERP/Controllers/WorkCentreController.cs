using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;
using AU_ERP.Validation;

namespace AU_ERP.Controllers
{
    [Authorize(Policy = "AdminDepartment")]
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

        private static string? NormalizeTimeUom(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var t = raw.Trim();
            if (WorkCenterTimeUom.IsAllowed(t)) return t;
            return WorkCenterTimeUom.FromMeasurementCode(t);
        }

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] WorkCentreDto dto, CancellationToken ct = default)
        {
            try
            {
                var name = dto.WorkCenterName?.Trim();
                if (string.IsNullOrEmpty(name))
                    return Json(new { success = false, message = "Work Centre Name is required." });

                var rawTu = dto.TimeUom?.Trim();
                string? timeUom = null;
                if (!string.IsNullOrEmpty(rawTu))
                {
                    timeUom = NormalizeTimeUom(dto.TimeUom);
                    if (timeUom == null || !WorkCenterTimeUom.IsAllowed(timeUom))
                        return Json(new { success = false, message = "Invalid time UOM. Use Min, Hr, or Day." });
                }

                var wc = new WorkCenterMasterSample
                {
                    WorkCenterName = name,
                    PlantID = dto.PlantID,
                    Description = dto.Description?.Trim(),
                    AvailableCapacity = dto.AvailableCapacity,
                    UtilizationPercentage = dto.UtilizationPercentage,
                    SetupTime = dto.SetupTime,
                    MachineTime = dto.MachineTime,
                    LaborTime = dto.LaborTime,
                    TimeUom = timeUom,
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
                    wc.MachineTime,
                    wc.LaborTime,
                    wc.TimeUom
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

                var rawTu = dto.TimeUom?.Trim();
                string? timeUom = null;
                if (!string.IsNullOrEmpty(rawTu))
                {
                    timeUom = NormalizeTimeUom(dto.TimeUom);
                    if (timeUom == null || !WorkCenterTimeUom.IsAllowed(timeUom))
                        return Json(new { success = false, message = "Invalid time UOM. Use Min, Hr, or Day." });
                }

                wc.WorkCenterName = name;
                wc.PlantID = dto.PlantID;
                wc.Description = dto.Description?.Trim();
                wc.AvailableCapacity = dto.AvailableCapacity;
                wc.UtilizationPercentage = dto.UtilizationPercentage;
                wc.SetupTime = dto.SetupTime;
                wc.MachineTime = dto.MachineTime;
                wc.LaborTime = dto.LaborTime;
                wc.TimeUom = timeUom;

                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "Work Centre Updated Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Update failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        private async Task<string?> GetWorkCentreDeletionBlockReasonAsync(int workCentreId, CancellationToken ct)
        {
            if (await _db.RoutingOperationsSamples.AsNoTracking()
                    .AnyAsync(o => o.WorkCenterID == workCentreId, ct))
            {
                return "This work centre cannot be deleted because it is still assigned to routing operation(s). "
                    + "Edit routings to use a different work centre or remove those operations, then try again.";
            }

            return null;
        }

        [HttpPost]
        public async Task<JsonResult> Delete(int id, CancellationToken ct = default)
        {
            try
            {
                var blocked = await GetWorkCentreDeletionBlockReasonAsync(id, ct);
                if (blocked != null)
                    return Json(new { success = false, message = blocked });

                var wc = await _db.WorkCenterMasterSamples
                    .FirstOrDefaultAsync(w => w.ID == id, ct);

                if (wc == null)
                    return Json(new { success = false, message = "Work Centre not found." });

                _db.WorkCenterMasterSamples.Remove(wc);
                await _db.SaveChangesAsync(ct);

                return Json(new { success = true, message = "Work Centre Deleted Successfully !" });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "work centre") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "work centre") });
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
        public decimal? MachineTime { get; set; }
        public decimal? LaborTime { get; set; }
        /// <summary>Min, Hr, or Day when any time field is set.</summary>
        public string? TimeUom { get; set; }
    }
}
