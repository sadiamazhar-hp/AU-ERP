using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;
using AU_ERP.Services;

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
