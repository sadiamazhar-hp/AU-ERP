using AU_ERP.Models;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AU_ERP.Controllers;

[Authorize(Policy = "StoreDepartment")]
public class StockMovementController : Controller
{
    private readonly AppDbContext _db;
    private readonly StockMovementService _movement;

    public StockMovementController(AppDbContext db, StockMovementService movement)
    {
        _db = db;
        _movement = movement;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var fromPlantId = UserPlantResolution.TryGetStorePlantId(User) ?? "";
        var rows = await _db.StockMovements.AsNoTracking()
            .Include(x => x.Material)
            .Include(x => x.QuantityUom)
            .Include(x => x.FromPlant)
            .Include(x => x.ToPlant)
            .OrderByDescending(x => x.Id)
            .Take(100)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        ViewBag.FromPlantId = fromPlantId;
        return View(rows);
    }

    [HttpGet]
    public async Task<JsonResult> Materials(string? q, CancellationToken ct = default)
    {
        var query = _db.CreateMaterialMaster.AsNoTracking()
            .Where(m => (m.MaterialTypeCode ?? "").Trim().ToUpper() == "FERT")
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim();
            query = query.Where(m => m.MaterialNumber.Contains(t) || (m.Description != null && m.Description.Contains(t)));
        }

        var items = await query.OrderBy(m => m.MaterialNumber)
            .Take(40)
            .Select(m => new { m.MaterialNumber, m.Description })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return Json(new { success = true, data = items });
    }

    [HttpGet]
    public async Task<JsonResult> ToPlants(CancellationToken ct = default)
    {
        var fromPlantId = UserPlantResolution.TryGetStorePlantId(User) ?? "";
        if (fromPlantId.Length == 0)
            return Json(new { success = false, message = "Your user has no Store plant assignment." });

        var data = await _db.PlantsSamples.AsNoTracking()
            .Where(p => p.PlantID != fromPlantId)
            .OrderBy(p => p.PlantID)
            .Select(p => new { plantId = p.PlantID, plantName = p.PlantName })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<JsonResult> QuantityPresent(string? materialNumber, string? grade, CancellationToken ct = default)
    {
        var fromPlantId = UserPlantResolution.TryGetStorePlantId(User) ?? "";
        if (fromPlantId.Length == 0)
            return Json(new { success = false, message = "Your user has no Store plant assignment." });

        var mat = (materialNumber ?? "").Trim();
        var g = (grade ?? "").Trim(); // optional; when blank, return total Active stock across grades
        if (mat.Length == 0)
            return Json(new { success = true, data = new { quantityPresent = 0m, materialNumber = mat } });

        var qty = await _movement.GetQuantityPresentAsync(mat, g, fromPlantId, ct).ConfigureAwait(false);
        return Json(new { success = true, data = new { quantityPresent = qty, materialNumber = mat } });
    }

    [HttpPost]
    public async Task<JsonResult> CreateTransfer([FromBody] CreateStockMovementDto? dto, CancellationToken ct = default)
    {
        if (dto == null)
            return Json(new { success = false, message = "Invalid request." });

        var fromPlantId = UserPlantResolution.TryGetStorePlantId(User) ?? "";
        if (fromPlantId.Length == 0)
            return Json(new { success = false, message = "Your user has no Store plant assignment." });

        var result = await _movement.MoveAsync(
            new StockMovementService.MoveRequest(
                dto.MaterialNumber ?? "",
                dto.Grade ?? "",
                fromPlantId,
                dto.ToPlantId ?? "",
                dto.QuantityMoved),
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            ct).ConfigureAwait(false);

        return Json(new { success = result.Success, message = result.Message, movementNumber = result.MovementNumber });
    }
}

public sealed class CreateStockMovementDto
{
    public string? MaterialNumber { get; set; }
    public string? Grade { get; set; }
    public string? ToPlantId { get; set; }
    public decimal QuantityMoved { get; set; }
}

