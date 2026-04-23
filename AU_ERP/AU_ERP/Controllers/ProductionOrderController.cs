using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;
using AU_ERP.Services;

namespace AU_ERP.Controllers
{
    [Authorize(Policy = "ProductionDepartment")]
    public class ProductionOrderController : Controller
    {
        private readonly AppDbContext _db;

        public ProductionOrderController(AppDbContext db) => _db = db;

        private static readonly string[] AllowedPriorities =
        {
            ProductionOrder.PriorityLow,
            ProductionOrder.PriorityMedium,
            ProductionOrder.PriorityHigh
        };

        private async Task<int> AllocateNextProductionNumberAsync(CancellationToken ct)
        {
            const int floor = 40000;
            if (!await _db.ProductionOrders.AnyAsync(ct))
                return floor;
            var max = await _db.ProductionOrders.AsNoTracking()
                .MaxAsync(p => p.ProductionNumber, ct);
            var next = max + 1;
            return next < floor ? floor : next;
        }

        private async Task<bool> FinishedItemValidAsync(string materialNumber, CancellationToken ct)
        {
            var key = materialNumber.Trim();
            return await _db.CreateMaterialMaster.AsNoTracking()
                .AnyAsync(m => m.MaterialNumber == key
                    && (m.MaterialTypeCode == "HALB" || m.MaterialTypeCode == "FERT"), ct);
        }

        public async Task<IActionResult> Index(string? q, string? status, string? priority, CancellationToken ct = default)
        {
            ViewData["Title"] = "Production Orders";

            var baseQuery = _db.ProductionOrders.AsNoTracking();

            var vm = new ProductionOrderPageVm
            {
                Q = q,
                Status = status,
                Priority = priority,
                CountPlanned = await baseQuery.CountAsync(p => p.Status == ProductionOrder.StatusPlanned, ct),
                CountReleased = await baseQuery.CountAsync(p => p.Status == ProductionOrder.StatusReleased, ct),
                CountInProgress = await baseQuery.CountAsync(p => p.Status == ProductionOrder.StatusInProgress, ct),
                CountCompleted = await baseQuery.CountAsync(p => p.Status == ProductionOrder.StatusCompleted, ct)
            };

            var filtered = _db.ProductionOrders.AsNoTracking()
                .Include(p => p.FinishedMaterial)
                .Include(p => p.Uom)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim();
                filtered = filtered.Where(p =>
                    p.FinishedMaterialNumber.Contains(t)
                    || (p.FinishedMaterial != null && p.FinishedMaterial.Description != null && p.FinishedMaterial.Description.Contains(t))
                    || (p.Remarks != null && p.Remarks.Contains(t)));
            }

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority) && !string.Equals(priority, "All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(p => p.Priority == priority);
            }

            vm.Items = await filtered
                .OrderByDescending(p => p.ProductionNumber)
                .ToListAsync(ct);

            return View(vm);
        }

        [HttpGet]
        public async Task<JsonResult> FinishedItems(CancellationToken ct = default)
        {
            var items = await _db.CreateMaterialMaster.AsNoTracking()
                .Where(m => m.MaterialTypeCode == "HALB" || m.MaterialTypeCode == "FERT")
                .OrderBy(m => m.MaterialNumber)
                .Select(m => new
                {
                    m.MaterialNumber,
                    m.Description,
                    m.MaterialTypeCode
                })
                .ToListAsync(ct);

            return Json(new { success = true, data = items });
        }

        [HttpGet]
        public async Task<JsonResult> Uoms(CancellationToken ct = default)
        {
            var items = await _db.UnitOfMeasurements.AsNoTracking()
                .OrderBy(u => u.Code)
                .Select(u => new { u.Id, u.Code, u.Description })
                .ToListAsync(ct);

            return Json(new { success = true, data = items });
        }

        /// <summary>Base UOM + alternates with <see cref="UnitConversion"/> for the finished material (same contract as Stock Overview).</summary>
        [HttpGet]
        public async Task<JsonResult> MaterialUomContext(string? materialNumber, int? includeUomIdForEdit, CancellationToken ct = default)
        {
            var (success, errorMessage, data) =
                await MaterialUomForMaterialHelper.TryBuildMaterialUomContextAsync(_db, materialNumber, includeUomIdForEdit, ct);
            if (!success)
                return Json(new { success = false, message = errorMessage });
            return Json(new { success = true, data });
        }

        [HttpGet]
        public async Task<JsonResult> GetForEdit(int id, CancellationToken ct = default)
        {
            var p = await _db.ProductionOrders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (p == null)
                return Json(new { success = false, message = "Production order not found." });

            return Json(new
            {
                success = true,
                data = new
                {
                    p.Id,
                    p.ProductionNumber,
                    p.FinishedMaterialNumber,
                    p.TargetQuantity,
                    p.UomId,
                    PlannedStartDate = p.PlannedStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    PlannedEndDate = p.PlannedEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    p.Priority,
                    p.Status,
                    p.Remarks,
                    p.ReleasedRoutingId
                }
            });
        }

        /// <summary>Read-only BOM lines scaled by order quantity: (target qty in finished base UOM / BOM BaseQty) × each BOM line qty.</summary>
        [HttpGet]
        public async Task<JsonResult> BomLines(int id, CancellationToken ct = default)
        {
            var p = await _db.ProductionOrders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (p == null)
                return Json(new { success = false, message = "Production order not found." });

            var (ok, err, lines) = await MrpExplosionService.GetBomLinesScaledForProductionOrderAsync(_db, p, ct);
            if (!ok)
                return Json(new { success = false, message = err });
            return Json(new { success = true, data = lines, finishedMaterialNumber = p.FinishedMaterialNumber });
        }

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] ProductionOrderCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                var err = ValidateDto(dto, out var priority, out var start, out var end);
                if (err != null)
                    return Json(new { success = false, message = err });

                if (!await FinishedItemValidAsync(dto.FinishedMaterialNumber!.Trim(), ct))
                    return Json(new { success = false, message = "Finished item must be a material of type HALB or FERT." });

                var uomOk = await _db.UnitOfMeasurements.AsNoTracking().AnyAsync(u => u.Id == dto.UomId, ct);
                if (!uomOk)
                    return Json(new { success = false, message = "Invalid UOM." });

                var uomRule = await MaterialUomForMaterialHelper.ValidateUomForMaterialAsync(
                    _db, dto.FinishedMaterialNumber.Trim(), dto.UomId, ct);
                if (uomRule != null)
                    return Json(new { success = false, message = uomRule });

                var mrp = await MrpExplosionService.RunAsync(
                    _db,
                    dto.FinishedMaterialNumber.Trim(),
                    dto.TargetQuantity,
                    dto.UomId,
                    requireFertMaterialOnly: false,
                    ct);
                if (!mrp.Success)
                    return Json(new { success = false, message = mrp.Message ?? "MRP validation failed." });
                if (!mrp.AllSatisfied)
                {
                    return Json(new
                    {
                        success = false,
                        message = MrpExplosionService.FormatShortageMessage(mrp),
                        mrpShortage = true,
                        mrpMaterialNumber = dto.FinishedMaterialNumber.Trim(),
                        mrpTargetQuantity = dto.TargetQuantity,
                        mrpUomId = dto.UomId
                    });
                }

                var number = await AllocateNextProductionNumberAsync(ct);
                var entity = new ProductionOrder
                {
                    ProductionNumber = number,
                    CreatedAt = DateTime.UtcNow,
                    FinishedMaterialNumber = dto.FinishedMaterialNumber.Trim(),
                    TargetQuantity = dto.TargetQuantity,
                    UomId = dto.UomId,
                    PlannedStartDate = start,
                    PlannedEndDate = end,
                    Priority = priority,
                    Status = ProductionOrder.StatusPlanned,
                    Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim()
                };

                await _db.ProductionOrders.AddAsync(entity, ct);
                await _db.SaveChangesAsync(ct);

                return Json(new { success = true, message = "Production order created." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Update([FromBody] ProductionOrderUpdateDto dto, CancellationToken ct = default)
        {
            try
            {
                var entity = await _db.ProductionOrders.FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
                if (entity == null)
                    return Json(new { success = false, message = "Production order not found." });

                if (entity.Status != ProductionOrder.StatusPlanned)
                    return Json(new { success = false, message = "Only planned orders can be edited." });

                var err = ValidateDto(dto, out var priority, out var start, out var end);
                if (err != null)
                    return Json(new { success = false, message = err });

                if (!await FinishedItemValidAsync(dto.FinishedMaterialNumber!.Trim(), ct))
                    return Json(new { success = false, message = "Finished item must be a material of type HALB or FERT." });

                var uomOk = await _db.UnitOfMeasurements.AsNoTracking().AnyAsync(u => u.Id == dto.UomId, ct);
                if (!uomOk)
                    return Json(new { success = false, message = "Invalid UOM." });

                var uomRule = await MaterialUomForMaterialHelper.ValidateUomForMaterialAsync(
                    _db, dto.FinishedMaterialNumber.Trim(), dto.UomId, ct);
                if (uomRule != null)
                    return Json(new { success = false, message = uomRule });

                entity.FinishedMaterialNumber = dto.FinishedMaterialNumber.Trim();
                entity.TargetQuantity = dto.TargetQuantity;
                entity.UomId = dto.UomId;
                entity.PlannedStartDate = start;
                entity.PlannedEndDate = end;
                entity.Priority = priority;
                entity.Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim();

                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "Production order updated." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Delete(int id, CancellationToken ct = default)
        {
            try
            {
                var entity = await _db.ProductionOrders.FirstOrDefaultAsync(p => p.Id == id, ct);
                if (entity == null)
                    return Json(new { success = false, message = "Production order not found." });

                if (entity.Status != ProductionOrder.StatusPlanned)
                    return Json(new { success = false, message = "Only planned orders can be deleted." });

                _db.ProductionOrders.Remove(entity);
                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "Production order deleted." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Release(int id, CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var entity = await _db.ProductionOrders
                    .Include(p => p.StageProgresses)
                    .FirstOrDefaultAsync(p => p.Id == id, ct);

                if (entity == null)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = "Production order not found." });
                }

                if (entity.Status != ProductionOrder.StatusPlanned)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = "Only planned orders can be released." });
                }

                if (entity.ReleasedRoutingId != null || entity.StageProgresses.Count > 0)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = "This order was already released." });
                }

                var mrp = await MrpExplosionService.RunAsync(
                    _db,
                    entity.FinishedMaterialNumber.Trim(),
                    entity.TargetQuantity,
                    entity.UomId,
                    requireFertMaterialOnly: false,
                    ct);
                if (!mrp.Success)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = mrp.Message ?? "MRP check failed; release aborted." });
                }

                if (!mrp.AllSatisfied)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new
                    {
                        success = false,
                        message = MrpExplosionService.FormatShortageMessage(mrp),
                        mrpShortage = true,
                        mrpMaterialNumber = entity.FinishedMaterialNumber.Trim(),
                        mrpTargetQuantity = entity.TargetQuantity,
                        mrpUomId = entity.UomId
                    });
                }

                var (bomOk, bomErr, bomLines) = await MrpExplosionService.BuildScaledBomLinesFromLiveBomAsync(_db, entity, ct);
                if (!bomOk)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = bomErr ?? "Could not build BOM snapshot for release." });
                }

                var routing = await _db.RoutingHeadersSamples
                    .Include(r => r.OperationHeaders)
                    .ThenInclude(oh => oh.RoutingOperationsSamples)
                    .Where(r => r.MaterialNumber == entity.FinishedMaterialNumber)
                    .OrderByDescending(r => r.ValidFrom)
                    .ThenByDescending(r => r.RoutingID)
                    .FirstOrDefaultAsync(ct);

                if (routing == null || routing.OperationHeaders == null || !routing.OperationHeaders.Any())
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = "No routing with operations is defined for this material." });
                }

                var orderedHeaders = routing.OperationHeaders.OrderBy(h => h.DisplayOrder).ToList();
                var now = DateTime.UtcNow;
                var isFirst = true;
                foreach (var h in orderedHeaders)
                {
                    decimal planned;
                    try
                    {
                        planned = RoutingPlannedHours.SumHeaderPlannedHours(h);
                    }
                    catch (InvalidOperationException ex)
                    {
                        await tx.RollbackAsync(ct);
                        return Json(new { success = false, message = ex.Message });
                    }

                    await _db.ProductionOrderStageProgresses.AddAsync(new ProductionOrderStageProgress
                    {
                        ProductionOrderId = entity.Id,
                        RoutingOperationHeaderId = h.OperationHeaderId,
                        SequenceOrder = h.DisplayOrder,
                        StageTitle = h.Title,
                        PlannedHours = planned,
                        StageStatus = isFirst
                            ? ProductionOrderStageProgress.StageInProgress
                            : ProductionOrderStageProgress.StagePending,
                        UpdatedAt = now
                    }, ct);
                    isFirst = false;
                }

                entity.ReleasedRoutingId = routing.RoutingID;
                entity.ReleasedBomSnapshotJson = MrpExplosionService.SerializeBomSnapshotLines(bomLines);
                entity.Status = ProductionOrder.StatusInProgress;
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return Json(new { success = true, message = "Production order released. Operation stages created from routing." });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        private string? ValidateDto(ProductionOrderCreateDto dto, out string priority, out DateTime start, out DateTime end)
        {
            priority = ProductionOrder.PriorityMedium;
            start = default;
            end = default;

            if (string.IsNullOrWhiteSpace(dto.FinishedMaterialNumber))
                return "Finished item is required.";

            if (dto.TargetQuantity <= 0)
                return "Target quantity must be greater than zero.";

            if (dto.UomId <= 0)
                return "UOM is required.";

            var pr = (dto.Priority ?? "").Trim();
            var match = AllowedPriorities.FirstOrDefault(p => p.Equals(pr, StringComparison.OrdinalIgnoreCase));
            if (match == null)
                return "Priority must be Low, Medium, or High.";
            priority = match;

            if (string.IsNullOrWhiteSpace(dto.PlannedStartDate)
                || !DateTime.TryParse(dto.PlannedStartDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out start))
                return "Planned start date is invalid.";

            if (string.IsNullOrWhiteSpace(dto.PlannedEndDate)
                || !DateTime.TryParse(dto.PlannedEndDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out end))
                return "Planned end date is invalid.";

            start = start.Date;
            end = end.Date;
            if (end < start)
                return "Planned end date cannot be before planned start date.";

            return null;
        }
    }

    public class ProductionOrderCreateDto
    {
        public string? FinishedMaterialNumber { get; set; }
        public int TargetQuantity { get; set; }
        public int UomId { get; set; }
        public string? PlannedStartDate { get; set; }
        public string? PlannedEndDate { get; set; }
        public string? Priority { get; set; }
        public string? Remarks { get; set; }
    }

    public class ProductionOrderUpdateDto : ProductionOrderCreateDto
    {
        public int Id { get; set; }
    }
}
