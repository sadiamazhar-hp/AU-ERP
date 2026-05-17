using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;
using AU_ERP.Services;
using AU_ERP.Validation;

namespace AU_ERP.Controllers;

[Authorize]
public class StockOverviewController : Controller
    {
        private readonly AppDbContext _db;
        private const string EmporiumPlantId = "Emp101";

        public StockOverviewController(AppDbContext db) => _db = db;

        private static decimal ComputeStockValue(decimal quantity, decimal standardCostPerUom)
            => Math.Round(quantity * standardCostPerUom, 2, MidpointRounding.AwayFromZero);

        private static bool IsEmporiumPlant(string? plantId)
            => string.Equals((plantId ?? "").Trim(), EmporiumPlantId, StringComparison.OrdinalIgnoreCase);

        public async Task<IActionResult> Index(string? q, string? materialTypeCode, int? uomId, CancellationToken ct = default)
        {
            ViewData["Title"] = "Stock Overview";

            var plantIds = UserPlantResolution.GetStorePlantIds(User);
            var missingPlant = plantIds.Count == 0 && User.HasClaim(AuClaimTypes.Department, "Store");

            var query = _db.StockInventoryLines.AsNoTracking()
                .Include(s => s.Material!)
                    .ThenInclude(m => m.MaterialType)
                .Include(s => s.QuantityUom)
                .AsQueryable();

            if (!missingPlant && plantIds.Count > 0)
                query = query.Where(s => plantIds.Contains(s.PlantID));

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim();
                query = query.Where(s =>
                    s.MaterialNumber.Contains(t)
                    || (s.Material != null && s.Material.Description != null && s.Material.Description.Contains(t)));
            }

            if (!string.IsNullOrWhiteSpace(materialTypeCode) && !string.Equals(materialTypeCode, "All", StringComparison.OrdinalIgnoreCase))
                query = query.Where(s => s.Material != null && s.Material.MaterialTypeCode == materialTypeCode);

            if (uomId is > 0)
                query = query.Where(s => s.QuantityUomId == uomId.Value);

            var items = await query
                .OrderByDescending(s => s.UpdatedAt)
                .ThenBy(s => s.MaterialNumber)
                .ThenBy(s => s.Grade)
                .ToListAsync(ct);

            var vm = new StockOverviewPageVm
            {
                Items = items,
                AssignedPlantIds = plantIds.ToList(),
                MissingStorePlantAssignment = missingPlant,
                PlantDisplay = plantIds.Count == 1 ? plantIds[0] : string.Join(", ", plantIds),
                CanAddStockEntry = plantIds.Any(p => !IsEmporiumPlant(p)),
                Q = q,
                MaterialTypeCode = string.IsNullOrWhiteSpace(materialTypeCode) ? "All" : materialTypeCode,
                UomId = uomId
            };

            return View(vm);
        }

        bool EnsureStorePlantForApi(ClaimsPrincipal user, out List<string> plantIds)
        {
            plantIds = UserPlantResolution.GetStorePlantIds(user).ToList();
            return plantIds.Count > 0;
        }

        [HttpGet]
        public async Task<JsonResult> MaterialTypes(CancellationToken ct = default)
        {
            var items = await _db.MaterialTypes.AsNoTracking()
                .OrderBy(t => t.MaterialTypeCode)
                .Select(t => new { t.MaterialTypeCode, t.Description })
                .ToListAsync(ct);
            return Json(new { success = true, data = items });
        }

        [HttpGet]
        public async Task<JsonResult> Materials(string? materialTypeCode, CancellationToken ct = default)
        {
            var q = _db.CreateMaterialMaster.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(materialTypeCode) && !string.Equals(materialTypeCode, "All", StringComparison.OrdinalIgnoreCase))
                q = q.Where(m => m.MaterialTypeCode == materialTypeCode);

            var items = await q
                .OrderBy(m => m.MaterialNumber)
                .Select(m => new { m.MaterialNumber, m.Description, m.MaterialTypeCode })
                .ToListAsync(ct);
            return Json(new { success = true, data = items });
        }

        /// <summary>
        /// Base UOM (from material master) plus alternate UOMs that have a <see cref="UnitConversion"/> row for this material.
        /// Optionally merges <paramref name="includeUomIdForEdit"/> into the list for edit screens (e.g. legacy rows).
        /// </summary>
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
        public async Task<JsonResult> Uoms(CancellationToken ct = default)
        {
            var items = await _db.UnitOfMeasurements.AsNoTracking()
                .OrderBy(u => u.Code)
                .Select(u => new { u.Id, u.Code, u.Description })
                .ToListAsync(ct);
            return Json(new { success = true, data = items });
        }

        [HttpGet]
        public async Task<JsonResult> GetForEdit(int id, CancellationToken ct = default)
        {
            if (!EnsureStorePlantForApi(User, out var plantIds))
                return Json(new { success = false, message = "Your user has no store plant assigned." });

            var row = await _db.StockInventoryLines.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && plantIds.Contains(s.PlantID), ct);
            if (row == null)
                return Json(new { success = false, message = "Stock entry not found." });

            var mat = await _db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == row.MaterialNumber, ct);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        row.Id,
                        row.MaterialNumber,
                        MaterialTypeCode = mat?.MaterialTypeCode,
                        row.PlantID,
                        row.Quantity,
                        row.QuantityUomId,
                        row.Status,
                        row.StandardCostPerUom,
                        row.StockValue,
                        row.Grade
                    }
                });
        }

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] StockInventoryLineDto dto, CancellationToken ct = default)
        {
            try
            {
                if (!EnsureStorePlantForApi(User, out var plantIds))
                    return Json(new { success = false, message = "Your user has no store plant assigned; an administrator must set the plant for your Store department." });

                var requestedPlantId = (dto.PlantID ?? "").Trim();
                if (requestedPlantId.Length == 0)
                    requestedPlantId = plantIds.FirstOrDefault(p => !IsEmporiumPlant(p)) ?? plantIds.First();

                if (!plantIds.Contains(requestedPlantId, StringComparer.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "You are not allowed to add stock in the selected plant." });
                if (IsEmporiumPlant(requestedPlantId))
                    return Json(new { success = false, message = "New stock addition is not allowed for Emporium Plant users." });

                var err = await ValidateDtoAsync(dto, ct);
                if (err != null)
                    return Json(new { success = false, message = err });

                var now = DateTime.UtcNow;
                var st = NormalizeStatus(dto.Status);
                var g = NormalizeGrade(dto.Grade);
                var key = dto.MaterialNumber!.Trim();
                var existing = await _db.StockInventoryLines
                    .FirstOrDefaultAsync(
                        s => s.MaterialNumber == key
                             && s.PlantID == requestedPlantId
                             && s.QuantityUomId == dto.QuantityUomId
                             && s.Status == st
                             && s.Grade == g
                             && (s.BatchOrLot ?? "") == "",
                        ct)
                    .ConfigureAwait(false);

                if (existing != null)
                {
                    existing.Quantity += dto.Quantity;
                    existing.StockValue = ComputeStockValue(existing.Quantity, existing.StandardCostPerUom);
                    existing.UpdatedAt = now;
                    await _db.SaveChangesAsync(ct);
                    return Json(new { success = true, message = "Quantity added to existing stock line (same material, UOM, status, and grade)." });
                }

                var stockValue = ComputeStockValue(dto.Quantity, dto.StandardCostPerUom);
                var entity = new StockInventoryLine
                {
                    MaterialNumber = key,
                    PlantID = requestedPlantId,
                    Quantity = dto.Quantity,
                    QuantityUomId = dto.QuantityUomId,
                    Status = st,
                    Grade = g,
                    StandardCostPerUom = dto.StandardCostPerUom,
                    StockValue = stockValue,
                    BatchOrLot = "",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _db.StockInventoryLines.AddAsync(entity, ct);
                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "Stock entry created." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Update([FromBody] StockInventoryLineUpdateDto dto, CancellationToken ct = default)
        {
            try
            {
                if (!EnsureStorePlantForApi(User, out var plantIds))
                    return Json(new { success = false, message = "Your user has no store plant assigned." });

                var entity = await _db.StockInventoryLines.FirstOrDefaultAsync(s => s.Id == dto.Id && plantIds.Contains(s.PlantID), ct);
                if (entity == null)
                    return Json(new { success = false, message = "Stock entry not found." });

                var err = await ValidateDtoAsync(dto, ct);
                if (err != null)
                    return Json(new { success = false, message = err });

                entity.MaterialNumber = dto.MaterialNumber!.Trim();
                entity.Quantity = dto.Quantity;
                entity.QuantityUomId = dto.QuantityUomId;
                entity.Status = NormalizeStatus(dto.Status);
                entity.Grade = NormalizeGrade(dto.Grade);
                entity.StandardCostPerUom = dto.StandardCostPerUom;
                entity.StockValue = ComputeStockValue(dto.Quantity, dto.StandardCostPerUom);
                entity.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "Stock entry updated." });
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
                if (!EnsureStorePlantForApi(User, out var plantIds))
                    return Json(new { success = false, message = "Your user has no store plant assigned." });

                var entity = await _db.StockInventoryLines.FirstOrDefaultAsync(s => s.Id == id && plantIds.Contains(s.PlantID), ct);
                if (entity == null)
                    return Json(new { success = false, message = "Stock entry not found." });

                _db.StockInventoryLines.Remove(entity);
                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "Stock entry deleted." });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "stock entry") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "stock entry") });
            }
        }

        private static string NormalizeStatus(string? status)
        {
            var s = (status ?? "").Trim();
            if (string.Equals(s, StockInventoryLine.StatusNotActive, StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Not Active", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Inactive", StringComparison.OrdinalIgnoreCase))
                return StockInventoryLine.StatusNotActive;
            return StockInventoryLine.StatusActive;
        }

        private static string NormalizeGrade(string? grade)
        {
            var s = (grade ?? "").Trim();
            if (s.Length > 32)
                s = s.Substring(0, 32);
            return s;
        }

        private async Task<string?> ValidateDtoAsync(StockInventoryLineDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.MaterialNumber))
                return "Material is required.";
            var key = dto.MaterialNumber.Trim();
            var mat = await _db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == key, ct);
            if (mat == null)
                return "Material does not exist.";
            if (DocumentQuantityRules.ValidatePositiveWhole(dto.Quantity, "Quantity") is { } qErr)
                return qErr;
            if (dto.QuantityUomId <= 0)
                return "Quantity UOM is required.";
            if (!await _db.UnitOfMeasurements.AsNoTracking().AnyAsync(u => u.Id == dto.QuantityUomId, ct))
                return "Invalid quantity UOM.";
            if (dto.StandardCostPerUom < 0)
                return "Standard cost per UOM cannot be negative.";
            var st = NormalizeStatus(dto.Status);
            if (st != StockInventoryLine.StatusActive && st != StockInventoryLine.StatusNotActive)
                return "Status must be Active or Not Active.";

            var uomErr = await MaterialUomForMaterialHelper.ValidateUomForMaterialAsync(_db, key, dto.QuantityUomId, ct);
            if (uomErr != null)
                return uomErr;

            return null;
        }
    }

    public class StockInventoryLineDto
    {
        public string? MaterialNumber { get; set; }
        public string? PlantID { get; set; }
        public decimal Quantity { get; set; }
        public int QuantityUomId { get; set; }
        public string? Status { get; set; }
        public decimal StandardCostPerUom { get; set; }
        /// <summary>Empty for ungraded; A/B/C/Scrap for finished goods from production.</summary>
        public string? Grade { get; set; }
    }

    public class StockInventoryLineUpdateDto : StockInventoryLineDto
    {
        public int Id { get; set; }
    }
