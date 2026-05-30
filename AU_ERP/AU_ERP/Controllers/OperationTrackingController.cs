using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;
using AU_ERP.Services;
using AU_ERP.Validation;

namespace AU_ERP.Controllers
{
    [Authorize(Policy = "ProductionDepartment")]
    public class OperationTrackingController : Controller
    {
        private readonly AppDbContext _db;

        public static readonly string[] WastageReasons = { "Breakage", "Trimming", "Other" };

        public OperationTrackingController(AppDbContext db) => _db = db;

        /// <summary>Dashboard: all production orders that have operation stages (released routing).</summary>
        public async Task<IActionResult> Index(int? productionOrderId, string? plantId, CancellationToken ct = default)
        {
            if (productionOrderId is > 0)
                return RedirectToAction(nameof(Details), new { productionOrderId = productionOrderId.Value });

            ViewData["Title"] = "Operation Tracking";

            var allPlants = await _db.PlantsSamples.AsNoTracking()
                .OrderBy(p => p.PlantName)
                .ToListAsync(ct);
            var plantScope = await SalesPlantAccess.ResolveAsync(_db, User, plantId, allPlants.Select(p => p.PlantID), ct);
            SalesPlantAccess.SetViewBag(this, plantScope, allPlants);
            ViewBag.ListPlantId = plantScope.EffectiveListPlantId ?? plantId ?? "";
            ViewBag.PlantFilterFormId = "otlListFilterForm";
            ViewBag.PlantHiddenInputId = "otlListPlant";
            ViewBag.PlantFilterModalId = "otlPlantFilterModal";
            ViewBag.PlantFilterModalPlantId = "otlModPlant";

            var ordersQuery = SalesPlantAccess.ApplyProductionOrderPlantFilter(
                    _db.ProductionOrders.AsNoTracking()
                        .Include(p => p.FinishedMaterial)
                        .Include(p => p.Uom)
                        .Include(p => p.StageProgresses)
                        .Include(p => p.Lines)
                        .Where(p => p.ReleasedRoutingId != null
                            && (p.Status == ProductionOrder.StatusReleased
                                || p.Status == ProductionOrder.StatusInProgress
                                || p.Status == ProductionOrder.StatusCompleted)),
                    plantScope);

            var orders = await ordersQuery
                .OrderByDescending(p => p.ProductionNumber)
                .ToListAsync(ct);

            var idsToMarkComplete = orders
                .Where(po =>
                {
                    var st = po.StageProgresses.ToList();
                    return AllStagesCompleted(st) && po.Status != ProductionOrder.StatusCompleted;
                })
                .Select(p => p.Id)
                .ToList();
            if (idsToMarkComplete.Count > 0)
            {
                await _db.ProductionOrders
                    .Where(p => idsToMarkComplete.Contains(p.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, ProductionOrder.StatusCompleted), ct);
            }

            var poIds = orders.Select(p => p.Id).ToList();
            var postedPoIds = await _db.GoodsProduceBatches.AsNoTracking()
                .Where(g => poIds.Contains(g.ProductionOrderId))
                .Select(g => g.ProductionOrderId)
                .ToListAsync(ct);
            var postedSet = postedPoIds.ToHashSet();

            var list = new List<OperationTrackingListItemVm>();
            foreach (var po in orders)
            {
                var stages = po.StageProgresses.OrderBy(s => s.SequenceOrder).ToList();
                var mat = po.FinishedMaterial;
                var itemLabel = mat != null
                    ? $"{po.FinishedMaterialNumber} — {mat.Description}"
                    : po.FinishedMaterialNumber;
                var lineLabels = po.Lines
                    .OrderBy(l => l.LineNo)
                    .Select(l => $"{l.MaterialNumber} ({l.PlannedQuantity:0.####})")
                    .ToList();
                if (lineLabels.Count == 0)
                    lineLabels.Add($"{po.FinishedMaterialNumber} ({po.TargetQuantity:0.####})");
                var active = FirstTrackableStage(stages);
                var effectiveStatus = AllStagesCompleted(stages)
                    ? ProductionOrder.StatusCompleted
                    : po.Status;

                list.Add(new OperationTrackingListItemVm
                {
                    ProductionOrderId = po.Id,
                    ProductionNumber = po.ProductionNumber,
                    FinishedItemLabel = itemLabel,
                    FinishedItemLines = lineLabels,
                    TargetQuantity = po.TargetQuantity,
                    UomCode = po.Uom?.Code,
                    Status = effectiveStatus,
                    OverallStatusLabel = DeriveOverallLabel(effectiveStatus, stages),
                    StagesTotal = stages.Count,
                    StagesCompleted = stages.Count(s =>
                        string.Equals(s.StageStatus, ProductionOrderStageProgress.StageCompleted, StringComparison.OrdinalIgnoreCase)),
                    ActiveStageTitle = active?.StageTitle,
                    GoodsReceiptPosted = postedSet.Contains(po.Id)
                });
            }

            return View("List", new OperationTrackingListVm
            {
                Orders = list,
                PlantId = plantScope.EffectiveListPlantId ?? (plantId ?? "")
            });
        }

        /// <summary>Single production order: stages, stepper, and update actions.</summary>
        public async Task<IActionResult> Details(int productionOrderId, CancellationToken ct = default)
        {
            ViewData["Title"] = "Production Stage Tracking";

            if (productionOrderId <= 0)
                return RedirectToAction(nameof(Index));

            var po = await _db.ProductionOrders.AsNoTracking()
                .Include(p => p.FinishedMaterial)
                .Include(p => p.Uom)
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == productionOrderId, ct);

            if (po == null)
                return RedirectToAction(nameof(Index));

            var linePlants = await _db.ProductionOrderLines.AsNoTracking()
                .Where(l => l.ProductionOrderId == po.Id)
                .Select(l => l.PlantId)
                .ToListAsync(ct);
            var allPlantIds = await _db.PlantsSamples.AsNoTracking()
                .Select(p => p.PlantID)
                .ToListAsync(ct);
            var readScope = await SalesPlantAccess.ResolveAsync(_db, User, null, allPlantIds, ct);
            if (!SalesPlantAccess.IsProductionOrderReadable(readScope, linePlants))
                return RedirectToAction(nameof(Index));

            var stages = await _db.ProductionOrderStageProgresses.AsNoTracking()
                .Where(s => s.ProductionOrderId == po.Id)
                .OrderBy(s => s.SequenceOrder)
                .ToListAsync(ct);

            if (AllStagesCompleted(stages) && po.Status != ProductionOrder.StatusCompleted)
            {
                await _db.ProductionOrders
                    .Where(p => p.Id == productionOrderId && p.Status != ProductionOrder.StatusCompleted)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, ProductionOrder.StatusCompleted), ct);
            }

            var effectiveOrderStatus = AllStagesCompleted(stages)
                ? ProductionOrder.StatusCompleted
                : po.Status;

            var mat = po.FinishedMaterial;
            var itemLabel = mat != null
                ? $"{mat.MaterialNumber} — {mat.Description}"
                : po.FinishedMaterialNumber;
            var lineLabels = po.Lines
                .OrderBy(l => l.LineNo)
                .Select(l => $"{l.MaterialNumber} ({l.PlannedQuantity:0.####})")
                .ToList();
            if (lineLabels.Count == 0)
                lineLabels.Add($"{po.FinishedMaterialNumber} ({po.TargetQuantity:0.####})");

            var active = FirstTrackableStage(stages);

            var machinesByStage = new Dictionary<int, List<OperationStageMachineLineVm>>();
            var headerIds = stages.Select(s => s.RoutingOperationHeaderId).Distinct().ToList();
            if (headerIds.Count > 0)
            {
                var headers = await _db.RoutingOperationHeadersSamples.AsNoTracking()
                    .Where(h => headerIds.Contains(h.OperationHeaderId))
                    .Include(h => h.RoutingOperationsSamples)
                        .ThenInclude(o => o.WorkCenter)
                    .ToListAsync(ct);
                var byHeader = headers.ToDictionary(h => h.OperationHeaderId);
                foreach (var st in stages)
                {
                    var lines = new List<OperationStageMachineLineVm>();
                    if (byHeader.TryGetValue(st.RoutingOperationHeaderId, out var hdr))
                    {
                        foreach (var op in hdr.RoutingOperationsSamples.OrderBy(o => o.OperationSequence))
                        {
                            var nm = op.WorkCenter?.WorkCenterName;
                            if (string.IsNullOrWhiteSpace(nm))
                                nm = op.Description;
                            if (string.IsNullOrWhiteSpace(nm))
                                nm = "—";
                            lines.Add(new OperationStageMachineLineVm
                            {
                                Sequence = op.OperationSequence,
                                DisplayName = nm,
                                LaborTime = op.LaborTime,
                                MachineTime = op.MachineTime,
                                TimeUom = op.TimeUom
                            });
                        }
                    }

                    machinesByStage[st.Id] = lines;
                }
            }

            var vm = new OperationTrackingPageVm
            {
                ProductionOrderId = po.Id,
                ProductionNumber = po.ProductionNumber,
                FinishedItemLabel = itemLabel,
                FinishedItemLines = lineLabels,
                TargetQuantity = po.TargetQuantity,
                UomCode = po.Uom?.Code,
                OverallStatusLabel = DeriveOverallLabel(effectiveOrderStatus, stages),
                OrderStatus = effectiveOrderStatus,
                Priority = po.Priority,
                PlannedStartDate = po.PlannedStartDate,
                PlannedEndDate = po.PlannedEndDate,
                ActiveStageTitle = active?.StageTitle,
                ActiveStageProgressId = active?.Id,
                StagesCompleted = stages.Count(s =>
                    string.Equals(s.StageStatus, ProductionOrderStageProgress.StageCompleted, StringComparison.OrdinalIgnoreCase)),
                StagesTotal = stages.Count,
                Stages = stages,
                MachinesByStageProgressId = machinesByStage
            };

            return View(vm);
        }

        private static bool AllStagesCompleted(List<ProductionOrderStageProgress> stages) =>
            stages.Count > 0 && stages.TrueForAll(s =>
                string.Equals(s.StageStatus, ProductionOrderStageProgress.StageCompleted, StringComparison.OrdinalIgnoreCase));

        private static bool IsTrackableStageStatus(string? status) =>
            !string.IsNullOrEmpty(status)
            && (string.Equals(status, ProductionOrderStageProgress.StageInProgress, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, ProductionOrderStageProgress.StageOnHold, StringComparison.OrdinalIgnoreCase));

        private static ProductionOrderStageProgress? FirstTrackableStage(IReadOnlyList<ProductionOrderStageProgress> stages) =>
            stages.OrderBy(s => s.SequenceOrder).FirstOrDefault(s => IsTrackableStageStatus(s.StageStatus));

        private static string DeriveOverallLabel(string status, List<ProductionOrderStageProgress> stages)
        {
            if (stages.Count == 0)
                return status switch
                {
                    ProductionOrder.StatusCompleted => "Completed",
                    ProductionOrder.StatusInProgress => "In progress",
                    ProductionOrder.StatusReleased => "Released",
                    ProductionOrder.StatusPlanned => "Planned",
                    _ => status
                };

            if (stages.TrueForAll(s =>
                    string.Equals(s.StageStatus, ProductionOrderStageProgress.StageCompleted, StringComparison.OrdinalIgnoreCase)))
                return "Completed";
            if (stages.Any(s =>
                    string.Equals(s.StageStatus, ProductionOrderStageProgress.StageOnHold, StringComparison.OrdinalIgnoreCase)))
                return "On hold";
            if (stages.Any(s =>
                    string.Equals(s.StageStatus, ProductionOrderStageProgress.StageInProgress, StringComparison.OrdinalIgnoreCase)))
                return "In progress";
            return "Pending";
        }

        /// <summary>Resolves the active (in-progress) stage for quick-update UIs (e.g. production order list modal).</summary>
        [HttpGet]
        public async Task<JsonResult> GetActiveStageForUpdate(int productionOrderId, CancellationToken ct = default)
        {
            var stage = await _db.ProductionOrderStageProgresses.AsNoTracking()
                .Include(s => s.ProductionOrder)
                .Where(s => s.ProductionOrderId == productionOrderId
                    && (s.StageStatus == ProductionOrderStageProgress.StageInProgress
                        || s.StageStatus == ProductionOrderStageProgress.StageOnHold))
                .OrderBy(s => s.SequenceOrder)
                .FirstOrDefaultAsync(ct);

            if (stage?.ProductionOrder == null)
                return Json(new { success = false, message = "No active stage to update. Open tracking to review completed orders." });

            return await BuildGetStageForUpdateJsonAsync(stage, ct);
        }

        [HttpGet]
        public Task<JsonResult> GetStageForUpdate(int productionOrderId, int stageProgressId, CancellationToken ct = default)
            => GetStageForUpdateCoreAsync(productionOrderId, stageProgressId, ct);

        private async Task<JsonResult> GetStageForUpdateCoreAsync(int productionOrderId, int stageProgressId, CancellationToken ct)
        {
            var stage = await _db.ProductionOrderStageProgresses.AsNoTracking()
                .Include(s => s.ProductionOrder)
                .FirstOrDefaultAsync(s => s.Id == stageProgressId && s.ProductionOrderId == productionOrderId, ct);

            if (stage?.ProductionOrder == null)
                return Json(new { success = false, message = "Stage not found." });

            if (!IsTrackableStageStatus(stage.StageStatus))
                return Json(new { success = false, message = "Only the active stage (in progress or on hold) can be updated here." });

            return await BuildGetStageForUpdateJsonAsync(stage, ct);
        }

        private async Task<JsonResult> BuildGetStageForUpdateJsonAsync(ProductionOrderStageProgress stage, CancellationToken ct)
        {
            var productionOrderId = stage.ProductionOrderId;
            var priorCount = await _db.ProductionOrderStageProgresses.AsNoTracking()
                .CountAsync(s => s.ProductionOrderId == productionOrderId && s.SequenceOrder < stage.SequenceOrder, ct);
            var stageIndex = priorCount + 1;

            decimal? defaultInput = null;
            if (stageIndex <= 1)
            {
                if (stage.ProductionOrderLineId is int lineId && lineId > 0)
                {
                    var lineQty = await _db.ProductionOrderLines.AsNoTracking()
                        .Where(l => l.Id == lineId)
                        .Select(l => (decimal?)l.PlannedQuantity)
                        .FirstOrDefaultAsync(ct);
                    defaultInput = lineQty ?? stage.ProductionOrder!.TargetQuantity;
                }
                else
                    defaultInput = stage.ProductionOrder!.TargetQuantity;
            }
            else
            {
                var prev = await _db.ProductionOrderStageProgresses.AsNoTracking()
                    .Where(s => s.ProductionOrderId == productionOrderId && s.SequenceOrder < stage.SequenceOrder)
                    .OrderByDescending(s => s.SequenceOrder)
                    .FirstOrDefaultAsync(ct);
                defaultInput = prev?.OutputQuantity;
            }

            var poNum = stage.ProductionOrder!.ProductionNumber;
            var title = string.IsNullOrWhiteSpace(stage.StageTitle) ? "Stage" : stage.StageTitle;

            return Json(new
            {
                success = true,
                data = new
                {
                    stage.Id,
                    stage.ProductionOrderId,
                    StageIndex = stageIndex,
                    StageTitle = title,
                    ProductionNumber = poNum,
                    DefaultInputQuantity = defaultInput,
                    stage.InputQuantity,
                    stage.OutputQuantity,
                    stage.WastageQuantity,
                    stage.ActualHours,
                    stage.WastageReason,
                    stage.WorkerOperator,
                    stage.Observations,
                    stage.StageStatus,
                    WastageReasons = WastageReasons,
                    MarkOptions = new[]
                    {
                        ProductionOrderStageProgress.StageInProgress,
                        ProductionOrderStageProgress.StageOnHold,
                        ProductionOrderStageProgress.StageCompleted
                    }
                }
            });
        }

        [HttpPost]
        public async Task<JsonResult> UpdateStage([FromBody] UpdateStageDto dto, CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                if (dto.ProductionOrderId <= 0 || dto.StageProgressId <= 0)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = "Invalid request." });
                }

                var stage = await _db.ProductionOrderStageProgresses
                    .Include(s => s.ProductionOrder)
                    .FirstOrDefaultAsync(s => s.Id == dto.StageProgressId && s.ProductionOrderId == dto.ProductionOrderId, ct);

                if (stage?.ProductionOrder == null)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = "Stage not found." });
                }

                if (!IsTrackableStageStatus(stage.StageStatus))
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = "Only the active stage (in progress or on hold) can be updated." });
                }

                var mark = (dto.MarkStageAs ?? "").Trim();
                string normalizedMark;
                if (string.Equals(mark, ProductionOrderStageProgress.StageCompleted, StringComparison.OrdinalIgnoreCase))
                    normalizedMark = ProductionOrderStageProgress.StageCompleted;
                else if (string.Equals(mark, ProductionOrderStageProgress.StageOnHold, StringComparison.OrdinalIgnoreCase))
                    normalizedMark = ProductionOrderStageProgress.StageOnHold;
                else if (string.Equals(mark, ProductionOrderStageProgress.StageInProgress, StringComparison.OrdinalIgnoreCase))
                    normalizedMark = ProductionOrderStageProgress.StageInProgress;
                else
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = "Mark stage as must be InProgress, OnHold, or Completed." });
                }

                if (dto.InputQuantity < 0 || dto.OutputQuantity < 0 || dto.ActualHours < 0)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = "Input, output, and actual hours must be non-negative." });
                }

                if (DocumentQuantityRules.ValidateNonNegativeWhole(dto.InputQuantity, "Input quantity") is { } iqMsg)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = iqMsg });
                }

                if (DocumentQuantityRules.ValidateNonNegativeWhole(dto.OutputQuantity, "Output quantity") is { } oqMsg)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = oqMsg });
                }

                if (dto.WastageQuantity.HasValue
                    && DocumentQuantityRules.ValidateNonNegativeWhole(dto.WastageQuantity.Value, "Wastage quantity") is { } wqMsg)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = wqMsg });
                }
                if (normalizedMark == ProductionOrderStageProgress.StageCompleted && dto.ActualHours <= 0)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = "Actual hours must be greater than zero when completing a stage." });
                }

                if (dto.OutputQuantity > dto.InputQuantity)
                {
                    await tx.RollbackAsync(ct);
                    return Json(new { success = false, message = "Output cannot exceed input." });
                }

                var derivedWastage = dto.InputQuantity - dto.OutputQuantity;
                if (dto.WastageQuantity.HasValue)
                {
                    var diff = Math.Abs(dto.WastageQuantity.Value - derivedWastage);
                    if (diff > 0.0001m)
                    {
                        await tx.RollbackAsync(ct);
                        return Json(new { success = false, message = "Wastage quantity must equal input minus output." });
                    }
                }

                var wastage = derivedWastage;

                stage.InputQuantity = dto.InputQuantity;
                stage.OutputQuantity = dto.OutputQuantity;
                stage.WastageQuantity = wastage;
                stage.ActualHours = dto.ActualHours;
                stage.WastageReason = string.IsNullOrWhiteSpace(dto.WastageReason) ? null : dto.WastageReason.Trim();
                stage.WorkerOperator = string.IsNullOrWhiteSpace(dto.WorkerOperator) ? null : dto.WorkerOperator.Trim();
                stage.Observations = string.IsNullOrWhiteSpace(dto.Observations) ? null : dto.Observations.Trim();
                stage.UpdatedAt = DateTime.UtcNow;

                if (normalizedMark == ProductionOrderStageProgress.StageCompleted)
                {
                    stage.StageStatus = ProductionOrderStageProgress.StageCompleted;

                    var next = await _db.ProductionOrderStageProgresses
                        .Where(s => s.ProductionOrderId == dto.ProductionOrderId
                            && s.SequenceOrder > stage.SequenceOrder
                            && s.StageStatus == ProductionOrderStageProgress.StagePending)
                        .OrderBy(s => s.SequenceOrder)
                        .FirstOrDefaultAsync(ct);

                    if (next != null)
                    {
                        next.StageStatus = ProductionOrderStageProgress.StageInProgress;
                        next.UpdatedAt = DateTime.UtcNow;
                    }

                    var allDone = !await _db.ProductionOrderStageProgresses
                        .AnyAsync(s => s.ProductionOrderId == dto.ProductionOrderId
                            && s.StageStatus != ProductionOrderStageProgress.StageCompleted, ct);

                    if (allDone)
                        stage.ProductionOrder.Status = ProductionOrder.StatusCompleted;
                }
                else if (normalizedMark == ProductionOrderStageProgress.StageOnHold)
                {
                    stage.StageStatus = ProductionOrderStageProgress.StageOnHold;
                }
                else
                {
                    stage.StageStatus = ProductionOrderStageProgress.StageInProgress;
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return Json(new { success = true, message = "Stage updated." });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetGoodsReceiptPrefill(int productionOrderId, CancellationToken ct = default)
        {
            if (productionOrderId <= 0)
                return Json(new { success = false, message = "Invalid production order." });

            var po = await _db.ProductionOrders.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productionOrderId, ct);
            if (po == null)
                return Json(new { success = false, message = "Production order not found." });

            var lastOut = await _db.ProductionOrderStageProgresses.AsNoTracking()
                .Where(s => s.ProductionOrderId == productionOrderId && s.OutputQuantity.HasValue)
                .OrderByDescending(s => s.SequenceOrder)
                .Select(s => s.OutputQuantity)
                .FirstOrDefaultAsync(ct);

            var defaultProduced = lastOut ?? po.TargetQuantity;

            return Json(new
            {
                success = true,
                data = new { productionNumber = po.ProductionNumber, defaultProducedQty = defaultProduced }
            });
        }

        [HttpPost]
        public async Task<JsonResult> PostGoodsReceipt(
            [FromBody] GoodsReceiptPostDto? dto,
            [FromServices] GoodsReceiptPostingService posting,
            CancellationToken ct = default)
        {
            var result = await posting.PostAsync(dto, ct).ConfigureAwait(false);
            return Json(new { success = result.Success, message = result.Message });
        }
    }

    public class UpdateStageDto
    {
        public int ProductionOrderId { get; set; }
        public int StageProgressId { get; set; }
        public decimal InputQuantity { get; set; }
        public decimal OutputQuantity { get; set; }
        /// <summary>Optional; when set must match input − output.</summary>
        public decimal? WastageQuantity { get; set; }
        public decimal ActualHours { get; set; }
        public string? WastageReason { get; set; }
        public string? WorkerOperator { get; set; }
        public string? MarkStageAs { get; set; }
        public string? Observations { get; set; }
    }
}
