using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;

namespace AU_ERP.Controllers
{
    [Authorize(Policy = "AdminDepartment")]
    public class RoutingController : Controller
    {
        private readonly AppDbContext _db;

        public RoutingController(AppDbContext db) => _db = db;

        private async Task PrepareViewBags(CancellationToken ct = default)
        {
            ViewBag.Plants = new SelectList(
                await _db.PlantsSamples.AsNoTracking().OrderBy(p => p.PlantID).ToListAsync(ct),
                "PlantID", "PlantName");

            ViewBag.WorkCentres = await _db.WorkCenterMasterSamples.AsNoTracking()
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
                .Include(r => r.OperationHeaders)
                    .ThenInclude(h => h.RoutingOperationsSamples)
                    .ThenInclude(o => o.WorkCenter)
                .OrderByDescending(r => r.RoutingID)
                .ToListAsync(ct);

            return View(list);
        }

        private async Task<string?> ResolveOpTimeUomFromWorkCentreAsync(int? workCenterId, CancellationToken ct)
        {
            if (workCenterId is null or <= 0)
                return null;
            return await _db.WorkCenterMasterSamples.AsNoTracking()
                .Where(w => w.ID == workCenterId.Value)
                .Select(w => w.TimeUom)
                .FirstOrDefaultAsync(ct);
        }

        /// <summary>Validates work-centre rows and appends new headers + lines (create routing).</summary>
        private async Task<JsonResult?> TryAppendOperationHeadersAsync(
            RoutingHeadersSample header,
            IReadOnlyList<RoutingOperationHeaderDto>? headerDtos,
            CancellationToken ct)
        {
            if (headerDtos == null || headerDtos.Count == 0)
                return Json(new { success = false, message = "Add at least one operation (title + work centre rows)." });

            var displayOrder = 10;
            foreach (var hDto in headerDtos)
            {
                var title = (hDto.Title ?? "").Trim();
                if (string.IsNullOrEmpty(title))
                    return Json(new { success = false, message = "Each operation must have a title." });

                var ops = hDto.Operations;
                if (ops == null || ops.Count == 0)
                    return Json(new { success = false, message = $"Operation \"{title}\" must have at least one work centre row." });

                var oh = new RoutingOperationHeaderSample
                {
                    Title = title,
                    DisplayOrder = displayOrder
                };
                displayOrder += 10;

                var err = await AppendWorkCentreLinesAsync(oh, ops, ct);
                if (err != null)
                    return err;

                header.OperationHeaders.Add(oh);
            }

            return null;
        }

        /// <summary>Adds work-centre lines to one operation header (new or existing).</summary>
        private async Task<JsonResult?> AppendWorkCentreLinesAsync(
            RoutingOperationHeaderSample oh,
            IReadOnlyList<RoutingOpDto> ops,
            CancellationToken ct)
        {
            var seq = 10;
            foreach (var op in ops)
            {
                var timeUom = await ResolveOpTimeUomFromWorkCentreAsync(op.WorkCenterID, ct);
                if (string.IsNullOrWhiteSpace(timeUom) || !WorkCenterTimeUom.IsAllowed(timeUom))
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
                        message = $"Work centre '{wcName ?? "—"}' has no time UOM (Min, Hr, Day). Set UOM on the work centre before using it in routing."
                    });
                }

                oh.RoutingOperationsSamples.Add(new RoutingOperationsSample
                {
                    WorkCenterID = op.WorkCenterID,
                    OperationSequence = seq,
                    Description = op.Description?.Trim(),
                    MachineTime = op.MachineTime,
                    LaborTime = op.LaborTime,
                    TimeUom = timeUom
                });
                seq += 10;
            }

            return null;
        }

        /// <summary>
        /// Updates routing operations without deleting headers still referenced by released production orders.
        /// New stages get new IDs; existing IDs are updated in place so PO stage snapshots stay valid.
        /// </summary>
        private async Task<JsonResult?> TryMergeOperationHeadersAsync(
            RoutingHeadersSample header,
            IReadOnlyList<RoutingOperationHeaderDto>? headerDtos,
            CancellationToken ct)
        {
            if (headerDtos == null || headerDtos.Count == 0)
                return Json(new { success = false, message = "Add at least one operation (title + work centre rows)." });

            var routingId = header.RoutingID;

            var opHeaderIdsOnRouting = await _db.RoutingOperationHeadersSamples.AsNoTracking()
                .Where(oh => oh.RoutingID == routingId)
                .Select(oh => oh.OperationHeaderId)
                .ToListAsync(ct);

            var inUseHeaderIds = await _db.ProductionOrderStageProgresses.AsNoTracking()
                .Where(s => opHeaderIdsOnRouting.Contains(s.RoutingOperationHeaderId))
                .Select(s => s.RoutingOperationHeaderId)
                .Distinct()
                .ToListAsync(ct);

            var dtoIdsWithValues = headerDtos
                .Where(d => d.OperationHeaderId.HasValue && d.OperationHeaderId.Value > 0)
                .Select(d => d.OperationHeaderId!.Value)
                .ToList();
            var dup = dtoIdsWithValues.GroupBy(x => x).FirstOrDefault(g => g.Count() > 1);
            if (dup != null)
                return Json(new { success = false, message = "Duplicate operation ids in the request. Refresh the page and try again." });

            var dtoReferencedIds = dtoIdsWithValues.ToHashSet();

            foreach (var existing in header.OperationHeaders.ToList())
            {
                if (dtoReferencedIds.Contains(existing.OperationHeaderId))
                    continue;

                if (inUseHeaderIds.Contains(existing.OperationHeaderId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cannot remove an operation that is still used by a released production order. " +
                                  "Either keep that operation in this routing, or create a new routing (new version / valid-from) for new releases while existing orders finish on the current snapshot."
                    });
                }

                _db.RoutingOperationHeadersSamples.Remove(existing);
            }

            for (var i = 0; i < headerDtos.Count; i++)
            {
                var hDto = headerDtos[i];
                var title = (hDto.Title ?? "").Trim();
                if (string.IsNullOrEmpty(title))
                    return Json(new { success = false, message = "Each operation must have a title." });

                var ops = hDto.Operations;
                if (ops == null || ops.Count == 0)
                    return Json(new { success = false, message = $"Operation \"{title}\" must have at least one work centre row." });

                var displayOrder = (i + 1) * 10;

                if (hDto.OperationHeaderId.HasValue && hDto.OperationHeaderId.Value > 0)
                {
                    var existingId = hDto.OperationHeaderId.Value;
                    var oh = header.OperationHeaders.FirstOrDefault(h => h.OperationHeaderId == existingId);
                    if (oh == null || oh.RoutingID != routingId)
                        return Json(new { success = false, message = "Operation not found or no longer belongs to this routing. Refresh the page and try again." });

                    oh.Title = title;
                    oh.DisplayOrder = displayOrder;

                    var oldLines = oh.RoutingOperationsSamples.ToList();
                    if (oldLines.Count > 0)
                        _db.RoutingOperationsSamples.RemoveRange(oldLines);
                    oh.RoutingOperationsSamples.Clear();

                    var err = await AppendWorkCentreLinesAsync(oh, ops, ct);
                    if (err != null)
                        return err;
                }
                else
                {
                    var oh = new RoutingOperationHeaderSample
                    {
                        RoutingID = routingId,
                        Title = title,
                        DisplayOrder = displayOrder
                    };
                    var err = await AppendWorkCentreLinesAsync(oh, ops, ct);
                    if (err != null)
                        return err;

                    header.OperationHeaders.Add(oh);
                }
            }

            return null;
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

                var err = await TryAppendOperationHeadersAsync(header, dto.OperationHeaders, ct);
                if (err != null)
                    return err;

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
                .Include(h => h.Material)
                .Include(h => h.OperationHeaders)
                    .ThenInclude(oh => oh.RoutingOperationsSamples)
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
                    MaterialDescription = r.Material != null ? r.Material.Description : null,
                    r.PlantID,
                    r.StatusID,
                    ValidFrom = r.ValidFrom.ToString("yyyy-MM-dd"),
                    OperationHeaders = r.OperationHeaders
                        .OrderBy(h => h.DisplayOrder)
                        .Select(h => new
                        {
                            h.OperationHeaderId,
                            h.Title,
                            h.DisplayOrder,
                            Operations = h.RoutingOperationsSamples
                                .OrderBy(o => o.OperationSequence)
                                .Select(o => new
                                {
                                    o.OpID,
                                    o.WorkCenterID,
                                    o.OperationSequence,
                                    o.Description,
                                    o.MachineTime,
                                    o.LaborTime,
                                    o.TimeUom
                                })
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
                    .Include(h => h.OperationHeaders)
                        .ThenInclude(oh => oh.RoutingOperationsSamples)
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

                var err = await TryMergeOperationHeadersAsync(header, dto.OperationHeaders, ct);
                if (err != null)
                    return err;

                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "Routing Updated Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Update failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        /// <summary>Replace only operation headers / work-centre lines; routing header fields unchanged.</summary>
        [HttpPost]
        public async Task<JsonResult> UpdateOperationHeaders([FromBody] RoutingOperationHeadersOnlyDto dto, CancellationToken ct = default)
        {
            try
            {
                var header = await _db.RoutingHeadersSamples
                    .Include(h => h.OperationHeaders)
                        .ThenInclude(oh => oh.RoutingOperationsSamples)
                    .FirstOrDefaultAsync(h => h.RoutingID == dto.RoutingID, ct);

                if (header == null)
                    return Json(new { success = false, message = "Routing not found." });

                var err = await TryMergeOperationHeadersAsync(header, dto.OperationHeaders, ct);
                if (err != null)
                    return err;

                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "Operations updated successfully." });
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
                    .FirstOrDefaultAsync(h => h.RoutingID == id, ct);

                if (header == null)
                    return Json(new { success = false, message = "Routing not found." });

                var opIdsUnderRouting = await _db.RoutingOperationHeadersSamples.AsNoTracking()
                    .Where(oh => oh.RoutingID == id)
                    .Select(oh => oh.OperationHeaderId)
                    .ToListAsync(ct);

                var stageRefs = await _db.ProductionOrderStageProgresses.AsNoTracking()
                    .AnyAsync(s => opIdsUnderRouting.Contains(s.RoutingOperationHeaderId), ct);

                if (stageRefs || await _db.ProductionOrders.AsNoTracking().AnyAsync(p => p.ReleasedRoutingId == id, ct))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cannot delete this routing because production orders still reference its operations. " +
                                  "Create a newer routing for new releases; existing orders keep this snapshot until completed."
                    });
                }

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
        public List<RoutingOperationHeaderDto>? OperationHeaders { get; set; }
    }

    public class RoutingOperationHeadersOnlyDto
    {
        public int RoutingID { get; set; }
        public List<RoutingOperationHeaderDto>? OperationHeaders { get; set; }
    }

    public class RoutingOperationHeaderDto
    {
        /// <summary>Existing DB id when editing; omit or 0 for a new operation block.</summary>
        public int? OperationHeaderId { get; set; }

        public string? Title { get; set; }
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
