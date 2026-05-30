using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;
using AU_ERP.Services;
using System.Globalization;

namespace AU_ERP.Controllers
{
    [Authorize(Policy = "ProductionDepartment")]
    public class MRPController : Controller
    {
        private readonly AppDbContext _db;
        private const string EmporiumPlantId = "Emp101";

        public MRPController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            ViewData["Title"] = "MRP";
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> FertMaterials(CancellationToken ct = default)
        {
            var items = await _db.CreateMaterialMaster.AsNoTracking()
                .Where(m => m.MaterialTypeCode == "FERT")
                .OrderBy(m => m.MaterialNumber)
                .Select(m => new { m.MaterialNumber, m.Description, m.MaterialTypeCode })
                .ToListAsync(ct);
            return Json(new { success = true, data = items });
        }

        /// <summary>Production plants available for MRP (Emporium excluded).</summary>
        [HttpGet]
        public async Task<JsonResult> Plants(CancellationToken ct = default)
        {
            var items = await _db.PlantsSamples.AsNoTracking()
                .Where(p => p.PlantID != EmporiumPlantId)
                .OrderBy(p => p.PlantName)
                .Select(p => new { plantId = p.PlantID, plantName = p.PlantName })
                .ToListAsync(ct);
            var defaultPlantId = await ResolveDefaultMrpPlantIdAsync(ct).ConfigureAwait(false);
            return Json(new { success = true, data = items, defaultPlantId });
        }

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
        /// Demand for a material from confirmed sales orders within a date range.
        /// Quantities are returned in the material base UOM (using unit conversions).
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> DemandForMaterial(string? materialNumber, string? from, string? to, CancellationToken ct = default)
        {
            var mat = (materialNumber ?? "").Trim();
            if (string.IsNullOrWhiteSpace(mat))
                return Json(new { success = false, message = "Material is required." });

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                return Json(new { success = false, message = "From date and To date are required." });

            if (!DateTime.TryParseExact(from.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate)
                || !DateTime.TryParseExact(to.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
                return Json(new { success = false, message = "Invalid date format." });

            fromDate = fromDate.Date;
            toDate = toDate.Date;
            if (toDate < fromDate)
                return Json(new { success = false, message = "To date must be on or after From date." });

            var matEntity = await _db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == mat, ct)
                .ConfigureAwait(false);
            if (matEntity == null)
                return Json(new { success = false, message = "Material not found." });

            var (baseUomId, baseUomErr) = await MaterialUomForMaterialHelper.ResolveBaseUomIdAsync(_db, matEntity, ct)
                .ConfigureAwait(false);
            if (baseUomId is null or <= 0)
                return Json(new { success = false, message = baseUomErr ?? "Base UOM not configured for this material." });

            var baseUomCode = await _db.UnitOfMeasurements.AsNoTracking()
                .Where(u => u.Id == baseUomId.Value)
                .Select(u => u.Code)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var lines = await _db.SalesOrderItems.AsNoTracking()
                .Where(i => i.MaterialNumber == mat)
                .Join(_db.SalesOrders.AsNoTracking(),
                    i => i.SalesOrderId,
                    o => o.Id,
                    (i, o) => new
                    {
                        o.Id,
                        o.OrderDate,
                        o.Status,
                        i.OrderQuantity,
                        i.QuantityUomId
                    })
                .Where(x => x.Status == SalesOrder.StatusConfirmed
                            && x.OrderDate >= fromDate
                            && x.OrderDate <= toDate)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (lines.Count == 0)
            {
                return Json(new
                {
                    success = true,
                    qtyBase = 0m,
                    baseUomId = baseUomId.Value,
                    baseUomCode = baseUomCode ?? "",
                    orderCount = 0,
                    lineCount = 0,
                    warnings = Array.Empty<string>()
                });
            }

            var warnings = new List<string>();
            decimal sumBase = 0m;
            foreach (var ln in lines)
            {
                if (ln.QuantityUomId is null or <= 0)
                {
                    warnings.Add("A sales order line has no UOM; it was skipped.");
                    continue;
                }

                var (ok, conv, err) = await UnitConversionMath.ConvertAsync(
                        _db, mat, ln.OrderQuantity, ln.QuantityUomId.Value, baseUomId.Value, ct)
                    .ConfigureAwait(false);
                if (!ok)
                {
                    warnings.Add(err ?? "A sales order line could not be converted to base UOM; it was skipped.");
                    continue;
                }
                sumBase += conv;
            }

            var orderCount = lines.Select(x => x.Id).Distinct().Count();
            return Json(new
            {
                success = true,
                qtyBase = decimal.Round(sumBase, 4, MidpointRounding.AwayFromZero),
                baseUomId = baseUomId.Value,
                baseUomCode = baseUomCode ?? "",
                orderCount,
                lineCount = lines.Count,
                warnings
            });
        }

        [HttpPost]
        public async Task<JsonResult> Run([FromBody] MrpRunRequestDto dto, CancellationToken ct = default)
        {
            var plant = (dto.PlantId ?? "").Trim();
            if (plant.Length == 0)
                plant = await ResolveDefaultMrpPlantIdAsync(ct).ConfigureAwait(false);
            if (plant.Length == 0)
                return Json(new MrpRunResponseDto { Success = false, Message = "No eligible plant found for MRP." });
            if (string.Equals(plant, EmporiumPlantId, StringComparison.OrdinalIgnoreCase))
                return Json(new MrpRunResponseDto { Success = false, Message = "Emporium plant is not allowed for this MRP flow." });
            var plantOk = await _db.PlantsSamples.AsNoTracking().AnyAsync(p => p.PlantID == plant, ct).ConfigureAwait(false);
            if (!plantOk)
                return Json(new MrpRunResponseDto { Success = false, Message = "Invalid plant." });
            
            var mat = (dto.MaterialNumber ?? "").Trim();
            var bomOptions = await BomMrpLookup.GetOptionsAsync(_db, mat, ct: ct);
            var bomCandidates = bomOptions.Select(x => x.BomId).ToList();
            if (bomCandidates.Count > 1 && (!dto.SelectedBomId.HasValue || dto.SelectedBomId.Value <= 0))
                return Json(new MrpRunResponseDto { Success = false, Message = "BOM selection is required." });
            if (dto.SelectedBomId.HasValue && dto.SelectedBomId.Value > 0 && !bomCandidates.Contains(dto.SelectedBomId.Value))
                return Json(new MrpRunResponseDto { Success = false, Message = "Selected BOM is deleted or not valid for MRP." });

            var result = await MrpExplosionService.RunAsync(
                _db,
                dto.MaterialNumber ?? "",
                dto.Quantity,
                dto.UomId,
                requireFertMaterialOnly: false,
                selectedBomId: dto.SelectedBomId,
                plantIdForStockOverride: plant,
                ct);
            return Json(result);
        }

        [HttpPost]
        public async Task<JsonResult> RunMulti([FromBody] MrpMultiRunRequestDto dto, CancellationToken ct = default)
        {
            var lines = dto?.Lines ?? new List<MrpMultiRunRequestLineDto>();
            if (lines.Count == 0)
                return Json(new MrpMultiRunResponseDto { Success = false, Message = "At least one material line is required." });

            var outLines = new List<MrpMultiRunResultLineDto>();
            var allSatisfied = true;
            var availableBaseByPlantAndMaterial = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            static string BuildAvailKey(string plantId, string materialNumber) =>
                $"{plantId.Trim().ToUpperInvariant()}|{materialNumber.Trim().ToUpperInvariant()}";

            async Task<decimal> ResolveOnHandBaseAsync(string plantId, string materialNumber)
            {
                var key = BuildAvailKey(plantId, materialNumber);
                if (availableBaseByPlantAndMaterial.TryGetValue(key, out var cached))
                    return cached;

                decimal totalBase = 0m;
                var stockLines = await _db.StockInventoryLines.AsNoTracking()
                    .Where(s => s.MaterialNumber == materialNumber
                                && s.Status == StockInventoryLine.StatusActive
                                && s.PlantID == plantId)
                    .Select(s => new { s.Quantity, s.QuantityUomId })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                foreach (var sl in stockLines)
                {
                    var (ok, qtyBase, _) = await UnitConversionMath.ToBaseAsync(
                            _db, materialNumber, sl.Quantity, sl.QuantityUomId, ct)
                        .ConfigureAwait(false);
                    if (ok)
                        totalBase += qtyBase;
                }

                availableBaseByPlantAndMaterial[key] = totalBase;
                return totalBase;
            }

            for (var i = 0; i < lines.Count; i++)
            {
                var ln = lines[i];
                var mat = (ln.MaterialNumber ?? "").Trim();
                var plant = (ln.PlantId ?? "").Trim();
                if (plant.Length == 0)
                    plant = await ResolveDefaultMrpPlantIdAsync(ct).ConfigureAwait(false);
                if (mat.Length == 0 || plant.Length == 0 || ln.Quantity <= 0 || ln.UomId <= 0)
                {
                    outLines.Add(new MrpMultiRunResultLineDto
                    {
                        LineNo = i + 1,
                        MaterialNumber = mat,
                        Description = ln.Description,
                        Quantity = ln.Quantity,
                        UomId = ln.UomId,
                        PlantId = plant,
                        Result = new MrpRunResponseDto { Success = false, Message = "Invalid line input." }
                    });
                    allSatisfied = false;
                    continue;
                }
                if (string.Equals(plant, EmporiumPlantId, StringComparison.OrdinalIgnoreCase))
                {
                    outLines.Add(new MrpMultiRunResultLineDto
                    {
                        LineNo = i + 1,
                        MaterialNumber = mat,
                        Description = ln.Description,
                        Quantity = ln.Quantity,
                        UomId = ln.UomId,
                        PlantId = plant,
                        Result = new MrpRunResponseDto { Success = false, Message = $"Emporium plant is not allowed for line {i + 1}." }
                    });
                    allSatisfied = false;
                    continue;
                }
                var plantOk = await _db.PlantsSamples.AsNoTracking().AnyAsync(p => p.PlantID == plant, ct).ConfigureAwait(false);
                if (!plantOk)
                {
                    outLines.Add(new MrpMultiRunResultLineDto
                    {
                        LineNo = i + 1,
                        MaterialNumber = mat,
                        Description = ln.Description,
                        Quantity = ln.Quantity,
                        UomId = ln.UomId,
                        PlantId = plant,
                        Result = new MrpRunResponseDto { Success = false, Message = $"Invalid plant for line {i + 1}." }
                    });
                    allSatisfied = false;
                    continue;
                }
                
                var bomOptions = await BomMrpLookup.GetOptionsAsync(_db, mat, ct: ct);
                if (bomOptions.Count == 0)
                {
                    outLines.Add(new MrpMultiRunResultLineDto
                    {
                        LineNo = i + 1,
                        MaterialNumber = mat,
                        Description = ln.Description,
                        Quantity = ln.Quantity,
                        UomId = ln.UomId,
                        PlantId = plant,
                        Result = new MrpRunResponseDto { Success = false, Message = $"No active valid BOM exists for line {i + 1}." }
                    });
                    allSatisfied = false;
                    continue;
                }
                var selectedBomId = ln.SelectedBomId;
                var selectedBomRef = (ln.SelectedBomAlternative ?? "").Trim();
                if (!selectedBomId.HasValue || selectedBomId.Value <= 0)
                {
                    if (bomOptions.Count == 1)
                    {
                        selectedBomId = bomOptions[0].BomId;
                        selectedBomRef = bomOptions[0].BomCode ?? "";
                    }
                    else
                    {
                        var def = bomOptions.FirstOrDefault(x => x.IsDefaultBom);
                        selectedBomId = def?.BomId;
                        if (def != null) selectedBomRef = def.BomCode ?? "";
                    }
                }
                else
                {
                    var picked = bomOptions.FirstOrDefault(x => x.BomId == selectedBomId.Value);
                    if (picked != null && string.IsNullOrWhiteSpace(selectedBomRef))
                        selectedBomRef = picked.BomCode ?? "";
                }
                if (!selectedBomId.HasValue || selectedBomId.Value <= 0)
                {
                    outLines.Add(new MrpMultiRunResultLineDto
                    {
                        LineNo = i + 1,
                        MaterialNumber = mat,
                        Description = ln.Description,
                        Quantity = ln.Quantity,
                        UomId = ln.UomId,
                        PlantId = plant,
                        Result = new MrpRunResponseDto { Success = false, Message = $"Select BOM for line {i + 1}." }
                    });
                    allSatisfied = false;
                    continue;
                }

                var result = await MrpExplosionService.RunAsync(
                    _db,
                    mat,
                    ln.Quantity,
                    ln.UomId,
                    requireFertMaterialOnly: false,
                    selectedBomId: selectedBomId,
                    plantIdForStockOverride: plant,
                    ct);

                // Cumulative stock check across all Multi-MRP lines in the same run.
                // This prevents false "success" when each line individually passes
                // but combined component demand exceeds available inventory.
                if (result.Success && result.Rows.Count > 0)
                {
                    var adjustedRows = new List<MrpRunResultRowDto>(result.Rows.Count);
                    foreach (var row in result.Rows)
                    {
                        var comp = (row.MaterialNumber ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(comp) || row.RequiredUomId <= 0 || row.RequiredQty <= 0)
                        {
                            adjustedRows.Add(row);
                            continue;
                        }

                        var availableBase = await ResolveOnHandBaseAsync(plant, comp).ConfigureAwait(false);
                        var availableBeforeBase = availableBase;
                        var (reqOk, requiredBase, _) = await UnitConversionMath.ToBaseAsync(
                                _db, comp, row.RequiredQty, row.RequiredUomId, ct)
                            .ConfigureAwait(false);
                        if (!reqOk)
                        {
                            row.Shortage = true;
                            adjustedRows.Add(row);
                            continue;
                        }

                        row.Shortage = availableBeforeBase + 0.0001m < requiredBase;
                        if (row.Shortage)
                            allSatisfied = false;
                        else
                            availableBaseByPlantAndMaterial[BuildAvailKey(plant, comp)] = availableBeforeBase - requiredBase;

                        var (onHandOk, onHandReqUom, _) = await UnitConversionMath.FromBaseAsync(
                                _db, comp, availableBeforeBase, row.RequiredUomId, ct)
                            .ConfigureAwait(false);
                        if (onHandOk)
                            row.OnHandQty = decimal.Round(onHandReqUom, 4, MidpointRounding.AwayFromZero);

                        adjustedRows.Add(row);
                    }

                    result.Rows = adjustedRows;
                    result.AllSatisfied = adjustedRows.Count > 0 && adjustedRows.TrueForAll(r => !r.Shortage);
                    if (!result.AllSatisfied)
                        result.Message = MrpExplosionService.FormatShortageMessage(result);
                }

                allSatisfied = allSatisfied && result.Success && result.AllSatisfied;
                outLines.Add(new MrpMultiRunResultLineDto
                {
                    LineNo = i + 1,
                    MaterialNumber = mat,
                    Description = ln.Description,
                    Quantity = ln.Quantity,
                    UomId = ln.UomId,
                    PlantId = plant,
                    Result = result,
                    SelectedBomId = selectedBomId,
                    SelectedBomAlternative = selectedBomRef
                });
            }

            return Json(new MrpMultiRunResponseDto
            {
                Success = outLines.Any(x => x.Result.Success),
                Message = allSatisfied ? "MRP completed for all lines." : "MRP completed with shortages/errors in one or more lines.",
                AllSatisfied = allSatisfied,
                Lines = outLines
            });
        }

        private async Task<string> ResolveDefaultMrpPlantIdAsync(CancellationToken ct)
        {
            var plant = await _db.PlantsSamples.AsNoTracking()
                .Where(p => p.PlantID != EmporiumPlantId)
                .OrderBy(p => p.PlantID)
                .Select(p => p.PlantID)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            return (plant ?? string.Empty).Trim();
        }
    }
}
