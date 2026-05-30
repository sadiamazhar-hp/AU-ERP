using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Configuration;
using AU_ERP.Models;
using AU_ERP.Services;
using AU_ERP.Validation;

namespace AU_ERP.Controllers
{
    [Authorize(Policy = "ProductionDepartment")]
    public class ProductionOrderController : Controller
    {
        private readonly AppDbContext _db;
        private readonly DocumentNumberAllocator _documentNumbers;
        private readonly GoodsIssueService _goodsIssueService;

        public ProductionOrderController(
            AppDbContext db,
            DocumentNumberAllocator documentNumbers,
            GoodsIssueService goodsIssueService)
        {
            _db = db;
            _documentNumbers = documentNumbers;
            _goodsIssueService = goodsIssueService;
        }

        private static readonly string[] AllowedPriorities =
        {
            ProductionOrder.PriorityLow,
            ProductionOrder.PriorityMedium,
            ProductionOrder.PriorityHigh
        };
        private const decimal InventoryEpsilon = 0.0001m;

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

            // List view: do not Include Lines or StageProgresses (cartesian / huge payloads → timeouts).
            // Stage completion for the Operation button is loaded in one lightweight follow-up query.
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

            var poIds = vm.Items.Select(p => p.Id).ToList();
            if (poIds.Count > 0)
            {
                var stageRows = await _db.ProductionOrderStageProgresses.AsNoTracking()
                    .Where(s => poIds.Contains(s.ProductionOrderId))
                    .Select(s => new { s.ProductionOrderId, s.StageStatus })
                    .ToListAsync(ct);
                foreach (var grp in stageRows.GroupBy(r => r.ProductionOrderId))
                    vm.OperationTrackingFullyCompletedByOrderId[grp.Key] =
                        grp.Any() && grp.All(r => r.StageStatus == ProductionOrderStageProgress.StageCompleted);

                var gis = await _db.GoodsIssueDocuments.AsNoTracking()
                    .Where(g => poIds.Contains(g.ProductionOrderId))
                    .Select(g => new { g.ProductionOrderId, g.Id, g.DocumentNumber, g.DispatchStatus, g.Status })
                    .ToListAsync(ct);
                foreach (var g in gis)
                {
                    vm.GoodsIssueIdByProductionOrderId[g.ProductionOrderId] = g.Id;
                    vm.GoodsIssueDocumentNumberByProductionOrderId[g.ProductionOrderId] = g.DocumentNumber;
                    vm.GoodsIssueDispatchStatusByProductionOrderId[g.ProductionOrderId] = g.DispatchStatus;
                    vm.GoodsIssueStatusByProductionOrderId[g.ProductionOrderId] = g.Status ?? GoodsIssueDocument.StatusPending;
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservationGoodsReceive(
            int id,
            [FromForm] string? returnQ,
            [FromForm] string? returnStatus,
            [FromForm] string? returnPriority,
            CancellationToken ct = default)
        {
            var gi = await _db.GoodsIssueDocuments.AsNoTracking()
                .FirstOrDefaultAsync(g => g.ProductionOrderId == id, ct);
            if (gi == null)
            {
                TempData["PoError"] = "No reservation goods issue exists for this production order.";
                return RedirectToPoList(returnQ, returnStatus, returnPriority);
            }

            var (ok, msg) = await _goodsIssueService.ReceiveGoodsAsync(
                gi.Id,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                null,
                ct);
            if (ok) TempData["PoMessage"] = msg;
            else TempData["PoError"] = msg;
            return RedirectToPoList(returnQ, returnStatus, returnPriority);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservationReleaseToOperations(
            int id,
            [FromForm] string? returnQ,
            [FromForm] string? returnStatus,
            [FromForm] string? returnPriority,
            CancellationToken ct = default)
        {
            var gi = await _db.GoodsIssueDocuments.AsNoTracking()
                .FirstOrDefaultAsync(g => g.ProductionOrderId == id, ct);
            if (gi == null)
            {
                TempData["PoError"] = "No reservation goods issue exists for this production order.";
                return RedirectToPoList(returnQ, returnStatus, returnPriority);
            }

            var (ok, msg) = await _goodsIssueService.ReleaseReservationToOperationsAsync(
                gi.Id,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                ct);
            if (ok) TempData["PoMessage"] = msg;
            else TempData["PoError"] = msg;
            return RedirectToPoList(returnQ, returnStatus, returnPriority);
        }

        private IActionResult RedirectToPoList(string? returnQ, string? returnStatus, string? returnPriority) =>
            RedirectToAction(nameof(Index), new
            {
                q = string.IsNullOrWhiteSpace(returnQ) ? null : returnQ.Trim(),
                status = returnStatus,
                priority = returnPriority
            });

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

        /// <summary>
        /// Active BOM headers for the finished material (same rules as <see cref="ValidateLineItemsAsync"/> BOM pick).
        /// Plant does not narrow the list — all plant-specific BOM variants are returned when valid.
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> BomCandidates(string? materialNumber, string? plantId, CancellationToken ct = default)
        {
            _ = plantId;
            var mn = (materialNumber ?? "").Trim();
            if (string.IsNullOrEmpty(mn))
            {
                return Json(new { success = true, data = new { boms = Array.Empty<object>(), requiresSelection = false } });
            }

            var matEntity = await _db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == mn, ct);
            var hdrType = (matEntity?.MaterialTypeCode ?? "").Trim().ToUpperInvariant();

            var options = await BomMrpLookup.GetOptionsAsync(_db, mn, hdrType, ct: ct);
            var boms = options.Select(h => new
            {
                bomId = h.BomId,
                code = h.BomCode ?? "",
                title = h.BomTitle ?? "",
                plant = h.Plant
            }).ToList();

            var requiresSelection = boms.Count > 1;
            return Json(new { success = true, data = new { boms, requiresSelection } });
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
                    p.ReleasedRoutingId,
                    Lines = await _db.ProductionOrderLines.AsNoTracking()
                        .Where(l => l.ProductionOrderId == p.Id)
                        .OrderBy(l => l.LineNo)
                        .Select(l => new
                        {
                            l.Id,
                            l.LineNo,
                            l.MaterialNumber,
                            l.MaterialDescription,
                            l.PlannedQuantity,
                            l.UomId,
                            l.PlantId,
                            l.SelectedBomId,
                            l.SelectedBomAlternative
                        })
                        .ToListAsync(ct)
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
            
            var poLines = await _db.ProductionOrderLines.AsNoTracking()
                .Where(l => l.ProductionOrderId == p.Id)
                .OrderBy(l => l.LineNo)
                .ToListAsync(ct);
            if (poLines.Count == 0)
            {
                var (okSingle, errSingle, linesSingle) = await MrpExplosionService.GetBomLinesScaledForProductionOrderAsync(_db, p, ct);
                if (!okSingle)
                    return Json(new { success = false, message = errSingle });
                return Json(new
                {
                    success = true,
                    data = new[]
                    {
                        new
                        {
                            lineNo = 1,
                            materialNumber = p.FinishedMaterialNumber,
                            quantity = p.TargetQuantity,
                            uomId = p.UomId,
                            bomLines = linesSingle
                        }
                    }
                });
            }
            
            var outLines = new List<object>();
            foreach (var ln in poLines)
            {
                var poLike = new ProductionOrder
                {
                    FinishedMaterialNumber = ln.MaterialNumber,
                    TargetQuantity = (int)Math.Round(ln.PlannedQuantity, MidpointRounding.AwayFromZero),
                    UomId = ln.UomId,
                    ReleasedBomSnapshotJson = p.ReleasedBomSnapshotJson,
                    SelectedBomId = ln.SelectedBomId,
                    SelectedBomAlternative = ln.SelectedBomAlternative
                };
                var (ok, err, lines) = await MrpExplosionService.GetBomLinesScaledForProductionOrderAsync(_db, poLike, ct);
                if (!ok)
                    return Json(new { success = false, message = $"Line {ln.LineNo}: {err}" });
                outLines.Add(new
                {
                    lineNo = ln.LineNo,
                    materialNumber = ln.MaterialNumber,
                    quantity = ln.PlannedQuantity,
                    uomId = ln.UomId,
                    bomLines = lines
                });
            }
            return Json(new { success = true, data = outLines });
        }

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] ProductionOrderCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                var err = ValidateDto(dto, out var priority, out var start, out var end);
                if (err != null)
                    return Json(new { success = false, message = err });
                
                var lineItems = BuildNormalizedLines(dto);
                var lineErr = await ValidateLineItemsAsync(lineItems, ct);
                if (lineErr != null)
                    return Json(new { success = false, message = lineErr });

                foreach (var li in lineItems)
                {
                    var mrp = await MrpExplosionService.RunAsync(
                        _db,
                        li.MaterialNumber,
                        li.PlannedQuantity,
                        li.UomId,
                        requireFertMaterialOnly: false,
                        selectedBomId: li.SelectedBomId,
                        plantIdForStockOverride: li.PlantId,
                        ct);
                    if (!mrp.Success)
                        return Json(new { success = false, message = $"Line {li.LineNo}: {mrp.Message ?? "MRP validation failed."}" });
                    if (!mrp.AllSatisfied)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Line {li.LineNo}: {MrpExplosionService.FormatShortageMessage(mrp)}",
                            mrpShortage = true,
                            mrpMaterialNumber = li.MaterialNumber,
                            mrpTargetQuantity = li.PlannedQuantity,
                            mrpUomId = li.UomId
                        });
                    }
                }

                var number = await AllocateNextProductionNumberAsync(ct);
                string prodDocNo;
                try
                {
                    prodDocNo = await _documentNumbers.AllocateAsync(ModuleKeys.ProductionOrder, ct).ConfigureAwait(false);
                }
                catch (DocumentIntegrationMissingException ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
                catch (DocumentIntegrationRangeExhaustedException ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }

                var first = lineItems.OrderBy(x => x.LineNo).First();
                var entity = new ProductionOrder
                {
                    ProductionNumber = number,
                    ProductionDocumentNumber = prodDocNo,
                    CreatedAt = DateTime.UtcNow,
                    FinishedMaterialNumber = first.MaterialNumber,
                    TargetQuantity = (int)Math.Round(first.PlannedQuantity, MidpointRounding.AwayFromZero),
                    UomId = first.UomId,
                    PlannedStartDate = start,
                    PlannedEndDate = end,
                    Priority = priority,
                    Status = ProductionOrder.StatusPlanned,
                    Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim(),
                    SelectedBomId = first.SelectedBomId,
                    SelectedBomAlternative = first.SelectedBomAlternative
                };
                foreach (var li in lineItems.OrderBy(x => x.LineNo))
                {
                    entity.Lines.Add(new ProductionOrderLine
                    {
                        LineNo = li.LineNo,
                        MaterialNumber = li.MaterialNumber,
                        MaterialDescription = li.MaterialDescription,
                        PlannedQuantity = li.PlannedQuantity,
                        UomId = li.UomId,
                        PlantId = li.PlantId,
                        SelectedBomId = li.SelectedBomId,
                        SelectedBomAlternative = li.SelectedBomAlternative
                    });
                }

                await _db.ProductionOrders.AddAsync(entity, ct);
                await _db.SaveChangesAsync(ct);

                _ = await _goodsIssueService.CreateOrOpenPendingAsync(
                    entity.Id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    ct);

                return Json(new { success = true, message = "Production order created." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> CreateFromMrpMulti([FromBody] ProductionOrderCreateFromMrpDto dto, CancellationToken ct = default)
        {
            try
            {
                var create = new ProductionOrderCreateDto
                {
                    PlannedStartDate = dto.PlannedStartDate,
                    PlannedEndDate = dto.PlannedEndDate,
                    Priority = dto.Priority,
                    Remarks = dto.Remarks,
                    Lines = (dto.Lines ?? new List<ProductionOrderLineInputDto>())
                };
                return await Create(create, ct);
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
                
                var lineItems = BuildNormalizedLines(dto);
                var lineErr = await ValidateLineItemsAsync(lineItems, ct);
                if (lineErr != null)
                    return Json(new { success = false, message = lineErr });

                var first = lineItems.OrderBy(x => x.LineNo).First();
                entity.FinishedMaterialNumber = first.MaterialNumber;
                entity.TargetQuantity = (int)Math.Round(first.PlannedQuantity, MidpointRounding.AwayFromZero);
                entity.UomId = first.UomId;
                entity.PlannedStartDate = start;
                entity.PlannedEndDate = end;
                entity.Priority = priority;
                entity.Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim();
                entity.SelectedBomId = first.SelectedBomId;
                entity.SelectedBomAlternative = first.SelectedBomAlternative;
                
                var existing = await _db.ProductionOrderLines
                    .Where(l => l.ProductionOrderId == entity.Id)
                    .ToListAsync(ct);
                _db.ProductionOrderLines.RemoveRange(existing);
                foreach (var li in lineItems.OrderBy(x => x.LineNo))
                {
                    _db.ProductionOrderLines.Add(new ProductionOrderLine
                    {
                        ProductionOrderId = entity.Id,
                        LineNo = li.LineNo,
                        MaterialNumber = li.MaterialNumber,
                        MaterialDescription = li.MaterialDescription,
                        PlannedQuantity = li.PlannedQuantity,
                        UomId = li.UomId,
                        PlantId = li.PlantId,
                        SelectedBomId = li.SelectedBomId,
                        SelectedBomAlternative = li.SelectedBomAlternative
                    });
                }

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

                var reservationGis = await _db.GoodsIssueDocuments
                    .Include(g => g.Lines)
                    .Where(x => x.ProductionOrderId == id)
                    .ToListAsync(ct);

                foreach (var g in reservationGis)
                {
                    if (string.Equals(g.Status, GoodsIssueDocument.StatusReceived, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(g.Status, GoodsIssueDocument.StatusCompleted, StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                "This planned order cannot be deleted because reservation goods have already been received from inventory. Remove or revert those postings before deleting."
                        });
                    }

                    if (g.Lines != null
                        && g.Lines.Any(l => l.IssuedQty > InventoryEpsilon))
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                "This planned order cannot be deleted because material has already been issued from stock on the linked reservation GI. Clear or reverse that issue first."
                        });
                    }
                }

                foreach (var g in reservationGis)
                    _db.GoodsIssueDocuments.Remove(g);

                var hasGrDoc = await _db.GoodReceiptDocuments.AsNoTracking()
                    .AnyAsync(x => x.ProductionOrderId == id, ct);
                if (hasGrDoc)
                {
                    return Json(new
                    {
                        success = false,
                        message = "This production order cannot be deleted because a Goods Receipt document exists for it."
                    });
                }

                var hasProduceBatch = await _db.GoodsProduceBatches.AsNoTracking()
                    .AnyAsync(x => x.ProductionOrderId == id, ct);
                if (hasProduceBatch)
                {
                    return Json(new
                    {
                        success = false,
                        message = "This production order cannot be deleted because produced batch records already exist for it."
                    });
                }

                _db.ProductionOrders.Remove(entity);
                await _db.SaveChangesAsync(ct);
                var deletedMsg = reservationGis.Count > 0
                    ? "Production order and linked reservation goods issue were deleted."
                    : "Production order deleted.";
                return Json(new { success = true, message = deletedMsg });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "production order") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "production order") });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Release(int id, CancellationToken ct = default)
        {
            await Task.CompletedTask;
            return Json(new
            {
                success = false,
                message = "Direct release is disabled. Use the RI action to create/complete Reservation Issue before operations."
            });
        }

        private async Task<string?> ConsumeBomInventoryForReleaseAsync(
            string plantId,
            IReadOnlyList<MrpRunResultRowDto>? mrpRows,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(plantId))
                return "Routing plant is required for BOM inventory deduction.";

            var reqRows = (mrpRows ?? new List<MrpRunResultRowDto>())
                .Where(r => !string.IsNullOrWhiteSpace(r.MaterialNumber) && r.RequiredUomId > 0 && r.RequiredQty > 0)
                .ToList();
            if (reqRows.Count == 0)
                return null;

            var grouped = reqRows
                .GroupBy(r => new { Mat = r.MaterialNumber.Trim(), r.RequiredUomId })
                .Select(g => new
                {
                    MaterialNumber = g.Key.Mat,
                    RequiredUomId = g.Key.RequiredUomId,
                    RequiredQty = g.Sum(x => x.RequiredQty)
                })
                .ToList();

            foreach (var req in grouped)
            {
                var rows = await _db.StockInventoryLines
                    .Where(s => s.MaterialNumber == req.MaterialNumber
                                && s.PlantID == plantId
                                && s.Status == StockInventoryLine.StatusActive
                                && s.Quantity > 0)
                    .OrderBy(s => s.UpdatedAt)
                    .ThenBy(s => s.Id)
                    .ToListAsync(ct);

                decimal available = 0m;
                foreach (var row in rows)
                {
                    var conv = await UnitConversionMath.ConvertAsync(
                        _db,
                        req.MaterialNumber,
                        row.Quantity,
                        row.QuantityUomId,
                        req.RequiredUomId,
                        ct);
                    if (conv.ok)
                        available += conv.quantityOut;
                }

                if (available + InventoryEpsilon < req.RequiredQty)
                {
                    return $"Cannot release: insufficient BOM stock for '{req.MaterialNumber}' at plant '{plantId}'. " +
                           $"Required {req.RequiredQty:0.####}, available {available:0.####}.";
                }

                decimal remaining = req.RequiredQty;
                var now = DateTime.UtcNow;
                foreach (var row in rows)
                {
                    if (remaining <= InventoryEpsilon)
                        break;

                    var convToReq = await UnitConversionMath.ConvertAsync(
                        _db,
                        req.MaterialNumber,
                        row.Quantity,
                        row.QuantityUomId,
                        req.RequiredUomId,
                        ct);
                    if (!convToReq.ok || convToReq.quantityOut <= 0)
                        continue;

                    var takeReqQty = Math.Min(convToReq.quantityOut, remaining);
                    var convBack = await UnitConversionMath.ConvertAsync(
                        _db,
                        req.MaterialNumber,
                        takeReqQty,
                        req.RequiredUomId,
                        row.QuantityUomId,
                        ct);
                    if (!convBack.ok || convBack.quantityOut <= 0)
                        continue;

                    var takeInRowUom = Math.Min(row.Quantity, convBack.quantityOut);
                    row.Quantity = Math.Round(row.Quantity - takeInRowUom, 4, MidpointRounding.AwayFromZero);
                    if (row.Quantity < 0)
                        row.Quantity = 0;
                    row.StockValue = Math.Round(row.Quantity * row.StandardCostPerUom, 2, MidpointRounding.AwayFromZero);
                    row.UpdatedAt = now;

                    var consumedReqQty = await UnitConversionMath.ConvertAsync(
                        _db,
                        req.MaterialNumber,
                        takeInRowUom,
                        row.QuantityUomId,
                        req.RequiredUomId,
                        ct);
                    if (consumedReqQty.ok)
                        remaining = Math.Max(0m, remaining - consumedReqQty.quantityOut);
                }

                if (remaining > InventoryEpsilon)
                {
                    return $"Cannot release: insufficient BOM stock for '{req.MaterialNumber}' during deduction. " +
                           $"Short by {remaining:0.####}.";
                }
            }

            return null;
        }

        private string? ValidateDto(ProductionOrderCreateDto dto, out string priority, out DateTime start, out DateTime end)
        {
            priority = ProductionOrder.PriorityMedium;
            start = default;
            end = default;

            if ((dto.Lines == null || dto.Lines.Count == 0) && string.IsNullOrWhiteSpace(dto.FinishedMaterialNumber))
                return "At least one finished material line is required.";

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
        
        private List<ProductionOrderLineInputDto> BuildNormalizedLines(ProductionOrderCreateDto dto)
        {
            var lines = (dto.Lines ?? new List<ProductionOrderLineInputDto>())
                .Where(l => !string.IsNullOrWhiteSpace(l.MaterialNumber))
                .Select((l, i) => new ProductionOrderLineInputDto
                {
                    LineNo = l.LineNo > 0 ? l.LineNo : i + 1,
                    MaterialNumber = (l.MaterialNumber ?? "").Trim(),
                    MaterialDescription = string.IsNullOrWhiteSpace(l.MaterialDescription) ? null : l.MaterialDescription.Trim(),
                    PlannedQuantity = l.PlannedQuantity,
                    UomId = l.UomId,
                    PlantId = string.IsNullOrWhiteSpace(l.PlantId) ? null : l.PlantId.Trim(),
                    SelectedBomId = l.SelectedBomId,
                    SelectedBomAlternative = string.IsNullOrWhiteSpace(l.SelectedBomAlternative) ? null : l.SelectedBomAlternative.Trim()
                })
                .ToList();
            if (lines.Count > 0)
                return lines;
            return new List<ProductionOrderLineInputDto>
            {
                new()
                {
                    LineNo = 1,
                    MaterialNumber = (dto.FinishedMaterialNumber ?? "").Trim(),
                    PlannedQuantity = dto.TargetQuantity,
                    UomId = dto.UomId,
                    SelectedBomId = dto.SelectedBomId,
                    SelectedBomAlternative = string.IsNullOrWhiteSpace(dto.SelectedBomAlternative)
                        ? null
                        : dto.SelectedBomAlternative.Trim()
                }
            };
        }
        
        private async Task<string?> ValidateLineItemsAsync(List<ProductionOrderLineInputDto> lines, CancellationToken ct)
        {
            if (lines.Count == 0)
                return "At least one finished material line is required.";
            foreach (var l in lines)
            {
                if (string.IsNullOrWhiteSpace(l.MaterialNumber))
                    return $"Line {l.LineNo}: material is required.";
                if (l.PlannedQuantity <= 0)
                    return $"Line {l.LineNo}: planned quantity must be greater than zero.";
                if (DocumentQuantityRules.ValidatePositiveWhole(l.PlannedQuantity, $"Line {l.LineNo} planned quantity") is { } pqWhole)
                    return pqWhole;
                if (l.UomId <= 0)
                    return $"Line {l.LineNo}: UOM is required.";
                if (!await FinishedItemValidAsync(l.MaterialNumber, ct))
                    return $"Line {l.LineNo}: material must be HALB or FERT.";
                var uomOk = await _db.UnitOfMeasurements.AsNoTracking().AnyAsync(u => u.Id == l.UomId, ct);
                if (!uomOk)
                    return $"Line {l.LineNo}: invalid UOM.";
                var uomRule = await MaterialUomForMaterialHelper.ValidateUomForMaterialAsync(_db, l.MaterialNumber, l.UomId, ct);
                if (uomRule != null)
                    return $"Line {l.LineNo}: {uomRule}";
                if (!string.IsNullOrWhiteSpace(l.PlantId))
                {
                    var plantOk = await _db.PlantsSamples.AsNoTracking().AnyAsync(p => p.PlantID == l.PlantId, ct);
                    if (!plantOk)
                        return $"Line {l.LineNo}: invalid plant.";
                }
                
                var matEntity = await _db.CreateMaterialMaster.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MaterialNumber == l.MaterialNumber, ct);
                var hdrType = (matEntity?.MaterialTypeCode ?? "").Trim().ToUpperInvariant();
                var bomOptions = await BomMrpLookup.GetOptionsAsync(_db, l.MaterialNumber, hdrType, ct: ct);
                var bomCandidates = bomOptions.Select(x => x.BomId).ToList();
                if (bomCandidates.Count > 1 && (!l.SelectedBomId.HasValue || l.SelectedBomId.Value <= 0))
                    return $"Line {l.LineNo}: BOM selection is required.";
                if (l.SelectedBomId.HasValue && l.SelectedBomId.Value > 0 && !bomCandidates.Contains(l.SelectedBomId.Value))
                    return $"Line {l.LineNo}: selected BOM is deleted or not valid for MRP.";
            }
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
        /// <summary>When posting a shorthand create (no <see cref="Lines"/>), sets BOM on the synthesized finished line.</summary>
        public int? SelectedBomId { get; set; }
        public string? SelectedBomAlternative { get; set; }
        public List<ProductionOrderLineInputDto> Lines { get; set; } = new();
    }

    public class ProductionOrderUpdateDto : ProductionOrderCreateDto
    {
        public int Id { get; set; }
    }
    
    public class ProductionOrderLineInputDto
    {
        public int LineNo { get; set; }
        public string MaterialNumber { get; set; } = "";
        public string? MaterialDescription { get; set; }
        public decimal PlannedQuantity { get; set; }
        public int UomId { get; set; }
        public string? PlantId { get; set; }
        public int? SelectedBomId { get; set; }
        public string? SelectedBomAlternative { get; set; }
    }
    
    public class ProductionOrderCreateFromMrpDto
    {
        public string? PlannedStartDate { get; set; }
        public string? PlannedEndDate { get; set; }
        public string? Priority { get; set; }
        public string? Remarks { get; set; }
        public List<ProductionOrderLineInputDto> Lines { get; set; } = new();
    }
}
