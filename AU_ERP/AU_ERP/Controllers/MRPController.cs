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

        public MRPController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            ViewData["Title"] = "MRP";
            ViewBag.MrpPlants = await _db.PlantsSamples.AsNoTracking()
                .OrderBy(p => p.PlantName).ToListAsync(ct).ConfigureAwait(false);
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> FertMaterials(CancellationToken ct = default)
        {
            var items = await _db.CreateMaterialMaster.AsNoTracking()
                .Where(m => m.MaterialTypeCode == "FERT" || m.MaterialTypeCode == "HALB")
                .OrderBy(m => m.MaterialNumber)
                .Select(m => new { m.MaterialNumber, m.Description, m.MaterialTypeCode })
                .ToListAsync(ct);
            return Json(new { success = true, data = items });
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
                return Json(new MrpRunResponseDto { Success = false, Message = "Plant is required to scope inventory for MRP." });
            var plantOk = await _db.PlantsSamples.AsNoTracking().AnyAsync(p => p.PlantID == plant, ct).ConfigureAwait(false);
            if (!plantOk)
                return Json(new MrpRunResponseDto { Success = false, Message = "Invalid plant." });

            var result = await MrpExplosionService.RunAsync(
                _db,
                dto.MaterialNumber ?? "",
                dto.Quantity,
                dto.UomId,
                requireFertMaterialOnly: false,
                plantIdForStockOverride: plant,
                ct);
            return Json(result);
        }
    }
}
