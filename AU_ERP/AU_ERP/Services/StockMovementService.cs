using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

public sealed class StockMovementService
{
    private readonly AppDbContext _db;

    public StockMovementService(AppDbContext db)
    {
        _db = db;
    }

    public sealed record MoveRequest(
        string MaterialNumber,
        string Grade,
        string FromPlantId,
        string ToPlantId,
        decimal QuantityMoved);

    public sealed record MoveResult(bool Success, string Message, string? MovementNumber = null);

    public async Task<decimal> GetQuantityPresentAsync(string materialNumber, string grade, string fromPlantId, CancellationToken ct = default)
    {
        var mat = (materialNumber ?? "").Trim();
        var g = NormalizeGrade(grade);
        var fromPlant = (fromPlantId ?? "").Trim();
        if (mat.Length == 0 || fromPlant.Length == 0)
            return 0;

        var q = _db.StockInventoryLines.AsNoTracking()
            .Where(s => s.MaterialNumber == mat
                        && s.PlantID == fromPlant
                        && s.Status == StockInventoryLine.StatusActive);
        if (g.Length > 0)
            q = q.Where(s => s.Grade == g);

        return await q.SumAsync(s => (decimal?)s.Quantity, ct).ConfigureAwait(false) ?? 0;
    }

    public async Task<MoveResult> MoveAsync(MoveRequest req, string? createdByUserId, CancellationToken ct = default)
    {
        var mat = (req.MaterialNumber ?? "").Trim();
        var g = NormalizeGrade(req.Grade);
        var fromPlant = (req.FromPlantId ?? "").Trim();
        var toPlant = (req.ToPlantId ?? "").Trim();
        var qty = req.QuantityMoved;

        if (mat.Length == 0) return new(false, "Material is required.");
        if (g.Length == 0) return new(false, "Grade is required.");
        if (fromPlant.Length == 0) return new(false, "From plant is required.");
        if (toPlant.Length == 0) return new(false, "To plant is required.");
        if (string.Equals(fromPlant, toPlant, StringComparison.OrdinalIgnoreCase))
            return new(false, "From Plant and To Plant cannot be the same.");
        if (qty <= 0) return new(false, "Quantity moved must be greater than zero.");

        var material = await _db.CreateMaterialMaster.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MaterialNumber == mat, ct)
            .ConfigureAwait(false);
        if (material == null) return new(false, "Material not found.");

        if (!await _db.PlantsSamples.AsNoTracking().AnyAsync(p => p.PlantID == fromPlant, ct).ConfigureAwait(false))
            return new(false, "From plant not found.");
        if (!await _db.PlantsSamples.AsNoTracking().AnyAsync(p => p.PlantID == toPlant, ct).ConfigureAwait(false))
            return new(false, "To plant not found.");

        var baseUomCode = (material.BaseUnitCode ?? "").Trim();
        if (baseUomCode.Length == 0) return new(false, "Material base UOM is required.");

        var uom = await _db.UnitOfMeasurements.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Code == baseUomCode, ct)
            .ConfigureAwait(false);
        if (uom == null) return new(false, $"Base UOM '{baseUomCode}' is not configured.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var sourceRows = await _db.StockInventoryLines
                .Where(s => s.MaterialNumber == mat
                            && s.PlantID == fromPlant
                            && s.Status == StockInventoryLine.StatusActive
                            && s.Grade == g
                            && s.Quantity > 0)
                .OrderBy(s => s.UpdatedAt)
                .ThenBy(s => s.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var available = sourceRows.Sum(x => x.Quantity);
            if (available < qty)
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new(false, $"Insufficient stock in From Plant. Available: {available:0.####}");
            }
            var availableCost = sourceRows.Sum(x => x.Quantity * x.StandardCostPerUom);

            var remaining = qty;
            var now = DateTime.UtcNow;
            foreach (var row in sourceRows)
            {
                if (remaining <= 0) break;
                var take = Math.Min(row.Quantity, remaining);
                row.Quantity = Math.Round(row.Quantity - take, 4, MidpointRounding.AwayFromZero);
                row.StockValue = Math.Round(row.Quantity * row.StandardCostPerUom, 2, MidpointRounding.AwayFromZero);
                row.UpdatedAt = now;
                remaining = Math.Round(remaining - take, 4, MidpointRounding.AwayFromZero);
            }

            var moveCost = available > 0 ? Math.Round(availableCost / available, 4, MidpointRounding.AwayFromZero) : 0m;

            var dest = await _db.StockInventoryLines.FirstOrDefaultAsync(s =>
                s.MaterialNumber == mat
                && s.PlantID == toPlant
                && s.QuantityUomId == uom.Id
                && s.Status == StockInventoryLine.StatusActive
                && s.Grade == g, ct).ConfigureAwait(false);

            if (dest == null)
            {
                _db.StockInventoryLines.Add(new StockInventoryLine
                {
                    MaterialNumber = mat,
                    PlantID = toPlant,
                    QuantityUomId = uom.Id,
                    Status = StockInventoryLine.StatusActive,
                    Grade = g,
                    Quantity = qty,
                    StandardCostPerUom = moveCost,
                    StockValue = Math.Round(qty * moveCost, 2, MidpointRounding.AwayFromZero),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                dest.Quantity += qty;
                if (dest.StandardCostPerUom <= 0 && moveCost > 0) dest.StandardCostPerUom = moveCost;
                dest.StockValue = Math.Round(dest.Quantity * dest.StandardCostPerUom, 2, MidpointRounding.AwayFromZero);
                dest.UpdatedAt = now;
            }

            var movementNo = await GenerateNextMovementNumberAsync(ct).ConfigureAwait(false);
            _db.StockMovements.Add(new StockMovement
            {
                MovementNumber = movementNo,
                MovementDate = DateTime.Today,
                MaterialNumber = mat,
                Grade = g,
                FromPlantId = fromPlant,
                ToPlantId = toPlant,
                QuantityMoved = qty,
                QuantityUomId = uom.Id,
                CreatedByUserId = string.IsNullOrWhiteSpace(createdByUserId) ? null : createdByUserId.Trim(),
                CreatedAt = now
            });

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return new(true, "Stock transferred successfully.", movementNo);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            return new(false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    private static string NormalizeGrade(string? grade)
    {
        var g = (grade ?? "").Trim();
        if (g.Equals("A", StringComparison.OrdinalIgnoreCase)) return "A";
        if (g.Equals("B", StringComparison.OrdinalIgnoreCase)) return "B";
        if (g.Equals("C", StringComparison.OrdinalIgnoreCase)) return "C";
        if (g.Equals("Scrap", StringComparison.OrdinalIgnoreCase)) return "Scrap";
        return g;
    }

    private async Task<string> GenerateNextMovementNumberAsync(CancellationToken ct)
    {
        const string prefix = "SM-";
        const int start = 1000;
        var list = await _db.StockMovements.AsNoTracking()
            .Select(m => m.MovementNumber)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var max = 0;
        foreach (var s in list)
        {
            var t = (s ?? "").Trim();
            if (!t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            if (int.TryParse(t[prefix.Length..], out var n))
                max = Math.Max(max, n);
        }

        return $"{prefix}{Math.Max(start, max + 1)}";
    }
}

