using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;

namespace AU_ERP.Controllers
{
    public class RoutingController : Controller
    {
        private readonly AppDbContext _db;

        public RoutingController(AppDbContext db) => _db = db;

        private async Task PrepareViewBags(CancellationToken ct = default)
        {
            ViewBag.Plants = new SelectList(
                await _db.PlantsSamples.AsNoTracking().OrderBy(p => p.PlantID).ToListAsync(ct),
                "PlantID", "PlantName");

            ViewBag.MaterialList = await _db.CreateMaterialMaster.AsNoTracking()
                .OrderBy(m => m.MaterialNumber).ToListAsync(ct);

            ViewBag.WorkCentres = await _db.WorkCenterMasterSamples.AsNoTracking()
                .Include(w => w.Uom)
                .OrderBy(w => w.WorkCenterName)
                .ToListAsync(ct);
        }

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            await PrepareViewBags(ct);

            var list = await _db.RoutingHeadersSamples
                .AsNoTracking()
                .Include(r => r.Material)
                .Include(r => r.Plant)
                .Include(r => r.RoutingOperationsSamples)
                .OrderByDescending(r => r.RoutingID)
                .ToListAsync(ct);

            return View(list);
        }

        private async Task<int?> ResolveOpUomFromWorkCentreAsync(int? workCenterId, CancellationToken ct)
        {
            if (workCenterId is null or <= 0)
                return null;
            return await _db.WorkCenterMasterSamples.AsNoTracking()
                .Where(w => w.ID == workCenterId.Value)
                .Select(w => w.UomId)
                .FirstOrDefaultAsync(ct);
        }

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] RoutingCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.MaterialNumber))
                    return Json(new { success = false, message = "Material is required." });

                if (string.IsNullOrWhiteSpace(dto.PlantID))
                    return Json(new { success = false, message = "Plant is required." });

                var matExists = await _db.CreateMaterialMaster
                    .AsNoTracking().AnyAsync(m => m.MaterialNumber == dto.MaterialNumber, ct);
                if (!matExists)
                    return Json(new { success = false, message = $"Material '{dto.MaterialNumber}' does not exist." });

                var header = new RoutingHeadersSample
                {
                    Title = dto.Title?.Trim(),
                    MaterialNumber = dto.MaterialNumber,
                    PlantID = dto.PlantID,
                    StatusID = dto.StatusID ?? 1,
                    ValidFrom = dto.ValidFrom ?? DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                if (dto.Operations != null)
                {
                    int seq = 10;
                    foreach (var op in dto.Operations)
                    {
                        var uomId = await ResolveOpUomFromWorkCentreAsync(op.WorkCenterID, ct);
                        if (!uomId.HasValue)
                        {
                            var wcName = op.WorkCenterID.HasValue
                                ? await _db.WorkCenterMasterSamples.AsNoTracking()
                                    .Where(w => w.ID == op.WorkCenterID.Value)
                                    .Select(w => w.WorkCenterName)
                                    .FirstOrDefaultAsync(ct)
                                : null;
                            return Json(new
                            {
                                success = false,
                                message = $"Work centre '{wcName ?? "—"}' has no UOM assigned. Set UOM on the work centre before using it in routing."
                            });
                        }

                        header.RoutingOperationsSamples.Add(new RoutingOperationsSample
                        {
                            WorkCenterID = op.WorkCenterID,
                            OperationSequence = seq,
                            Description = op.Description?.Trim(),
                            MachineTime = op.MachineTime,
                            LaborTime = op.LaborTime,
                            UomId = uomId
                        });
                        seq += 10;
                    }
                }

                await _db.RoutingHeadersSamples.AddAsync(header, ct);
                await _db.SaveChangesAsync(ct);

                return Json(new { success = true, message = "Routing Created Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Save failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetForEdit(int id, CancellationToken ct = default)
        {
            var r = await _db.RoutingHeadersSamples
                .AsNoTracking()
                .Include(h => h.RoutingOperationsSamples)
                    .ThenInclude(o => o.Uom)
                .FirstOrDefaultAsync(h => h.RoutingID == id, ct);

            if (r == null)
                return Json(new { success = false, message = "Routing not found." });

            return Json(new
            {
                success = true,
                data = new
                {
                    r.RoutingID,
                    r.Title,
                    r.MaterialNumber,
                    r.PlantID,
                    r.StatusID,
                    ValidFrom = r.ValidFrom.ToString("yyyy-MM-dd"),
                    Operations = r.RoutingOperationsSamples
                        .OrderBy(o => o.OperationSequence)
                        .Select(o => new
                        {
                            o.OpID,
                            o.WorkCenterID,
                            o.OperationSequence,
                            o.Description,
                            o.MachineTime,
                            o.LaborTime,
                            o.UomId,
                            UomCode = o.Uom != null ? o.Uom.Code : null
                        })
                }
            });
        }

        [HttpPost]
        public async Task<JsonResult> Update([FromBody] RoutingCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                var header = await _db.RoutingHeadersSamples
                    .Include(h => h.RoutingOperationsSamples)
                    .FirstOrDefaultAsync(h => h.RoutingID == dto.RoutingID, ct);

                if (header == null)
                    return Json(new { success = false, message = "Routing not found." });

                if (string.IsNullOrWhiteSpace(dto.MaterialNumber))
                    return Json(new { success = false, message = "Material is required." });

                header.Title = dto.Title?.Trim();
                header.MaterialNumber = dto.MaterialNumber;
                header.PlantID = dto.PlantID;
                header.StatusID = dto.StatusID;
                header.ValidFrom = dto.ValidFrom ?? header.ValidFrom;

                _db.RoutingOperationsSamples.RemoveRange(header.RoutingOperationsSamples);

                if (dto.Operations != null)
                {
                    int seq = 10;
                    foreach (var op in dto.Operations)
                    {
                        var uomId = await ResolveOpUomFromWorkCentreAsync(op.WorkCenterID, ct);
                        if (!uomId.HasValue)
                        {
                            var wcName = op.WorkCenterID.HasValue
                                ? await _db.WorkCenterMasterSamples.AsNoTracking()
                                    .Where(w => w.ID == op.WorkCenterID.Value)
                                    .Select(w => w.WorkCenterName)
                                    .FirstOrDefaultAsync(ct)
                                : null;
                            return Json(new
                            {
                                success = false,
                                message = $"Work centre '{wcName ?? "—"}' has no UOM assigned. Set UOM on the work centre before using it in routing."
                            });
                        }

                        header.RoutingOperationsSamples.Add(new RoutingOperationsSample
                        {
                            WorkCenterID = op.WorkCenterID,
                            OperationSequence = seq,
                            Description = op.Description?.Trim(),
                            MachineTime = op.MachineTime,
                            LaborTime = op.LaborTime,
                            UomId = uomId
                        });
                        seq += 10;
                    }
                }

                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "Routing Updated Successfully !" });
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
                var header = await _db.RoutingHeadersSamples
                    .Include(h => h.RoutingOperationsSamples)
                    .FirstOrDefaultAsync(h => h.RoutingID == id, ct);

                if (header == null)
                    return Json(new { success = false, message = "Routing not found." });

                _db.RoutingOperationsSamples.RemoveRange(header.RoutingOperationsSamples);
                _db.RoutingHeadersSamples.Remove(header);
                await _db.SaveChangesAsync(ct);

                return Json(new { success = true, message = "Routing Deleted Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }

    public class RoutingCreateDto
    {
        public int RoutingID { get; set; }
        public string? Title { get; set; }
        public string? MaterialNumber { get; set; }
        public string? PlantID { get; set; }
        public int? StatusID { get; set; }
        public DateTime? ValidFrom { get; set; }
        public List<RoutingOpDto>? Operations { get; set; }
    }

    public class RoutingOpDto
    {
        public int? WorkCenterID { get; set; }
        public string? Description { get; set; }
        public decimal? MachineTime { get; set; }
        public decimal? LaborTime { get; set; }
    }
}
