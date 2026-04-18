using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;
using AU_ERP.Services;

namespace AU_ERP.Controllers
{
    public class MRPController : Controller
    {
        private readonly AppDbContext _db;

        public MRPController(AppDbContext db) => _db = db;

        public IActionResult Index()
        {
            ViewData["Title"] = "MRP";
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
            var result = await MrpExplosionService.RunAsync(
                _db,
                dto.MaterialNumber ?? "",
                dto.Quantity,
                dto.UomId,
                requireFertMaterialOnly: false,
                ct);
            return Json(result);
        }
    }
}
