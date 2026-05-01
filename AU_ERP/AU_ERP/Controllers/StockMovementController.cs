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
        var assignedPlantIds = UserPlantResolution.GetStorePlantIds(User);
        var fromPlantOptions = await _db.PlantsSamples.AsNoTracking()
            .Where(p => assignedPlantIds.Contains(p.PlantID))
            .OrderBy(p => p.PlantID)
            .Select(p => new { plantId = p.PlantID, plantName = p.PlantName })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var fromPlantId = fromPlantOptions.FirstOrDefault()?.plantId ?? "";
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
        ViewBag.FromPlantOptions = fromPlantOptions;
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
    public async Task<JsonResult> ToPlants(string? fromPlantId, CancellationToken ct = default)
    {
        var fromId = (fromPlantId ?? "").Trim();
        var assignedPlantIds = UserPlantResolution.GetStorePlantIds(User);
        if (assignedPlantIds.Count == 0)
            return Json(new { success = false, message = "Your user has no Store plant assignment." });
        if (fromId.Length > 0 && !assignedPlantIds.Contains(fromId, StringComparer.OrdinalIgnoreCase))
            return Json(new { success = false, message = "You are not allowed to move stock from this plant." });

        var data = await _db.PlantsSamples.AsNoTracking()
            .Where(p => p.PlantID != fromId)
            .OrderBy(p => p.PlantID)
            .Select(p => new { plantId = p.PlantID, plantName = p.PlantName })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<JsonResult> QuantityPresent(string? materialNumber, string? grade, string? fromPlantId, CancellationToken ct = default)
    {
        var fromId = (fromPlantId ?? "").Trim();
        var assignedPlantIds = UserPlantResolution.GetStorePlantIds(User);
        if (assignedPlantIds.Count == 0)
            return Json(new { success = false, message = "Your user has no Store plant assignment." });
        if (fromId.Length == 0)
            return Json(new { success = false, message = "From plant is required." });
        if (!assignedPlantIds.Contains(fromId, StringComparer.OrdinalIgnoreCase))
            return Json(new { success = false, message = "You are not allowed to move stock from this plant." });

        var mat = (materialNumber ?? "").Trim();
        var g = (grade ?? "").Trim(); // optional; when blank, return total Active stock across grades
        if (mat.Length == 0)
            return Json(new { success = true, data = new { quantityPresent = 0m, materialNumber = mat } });

        var qty = await _movement.GetQuantityPresentAsync(mat, g, fromId, ct).ConfigureAwait(false);
        return Json(new { success = true, data = new { quantityPresent = qty, materialNumber = mat } });
    }

    [HttpPost]
    public async Task<JsonResult> CreateTransfer([FromBody] CreateStockMovementDto? dto, CancellationToken ct = default)
    {
        if (dto == null)
            return Json(new { success = false, message = "Invalid request." });

        var fromPlantId = (dto.FromPlantId ?? "").Trim();
        var assignedPlantIds = UserPlantResolution.GetStorePlantIds(User);
        if (assignedPlantIds.Count == 0)
            return Json(new { success = false, message = "Your user has no Store plant assignment." });
        if (fromPlantId.Length == 0)
            return Json(new { success = false, message = "From plant is required." });
        if (!assignedPlantIds.Contains(fromPlantId, StringComparer.OrdinalIgnoreCase))
            return Json(new { success = false, message = "You are not allowed to move stock from this plant." });

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
    public string? FromPlantId { get; set; }
    public string? ToPlantId { get; set; }
    public decimal QuantityMoved { get; set; }
}

