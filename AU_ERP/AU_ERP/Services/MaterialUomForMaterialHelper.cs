using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;

namespace AU_ERP.Services
{
    public static class MaterialUomForMaterialHelper
    {
        public const string UomNotAllowedMessage =
            "The selected unit of measure must be the material base unit or an alternate unit with a unit conversion on the material master.";

        /// <summary>Resolves base UOM id from <see cref="CreateMaterialMaster.BaseUnitCode"/> (case-insensitive match to <see cref="UnitOfMeasurement.Code"/>).</summary>
        public static async Task<(int? baseUomId, string? setupError)> ResolveBaseUomIdAsync(
            AppDbContext db,
            CreateMaterialMaster mat,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(mat.BaseUnitCode))
                return (null, "Set a base unit of measure on the material master before using this material.");

            var code = mat.BaseUnitCode.Trim();
            var baseUom = await db.UnitOfMeasurements.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Code != null && u.Code.Trim().ToLower() == code.ToLower(), ct);
            if (baseUom == null)
                return (null, $"No unit of measurement matches the material base unit code '{code}'. Add the UOM or correct the material base unit.");
            return (baseUom.Id, null);
        }

        /// <returns>null if valid; otherwise an error message.</returns>
        public static async Task<string?> ValidateUomForMaterialAsync(
            AppDbContext db,
            string materialNumber,
            int uomId,
            CancellationToken ct = default)
        {
            var key = materialNumber.Trim();
            var mat = await db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == key, ct);
            if (mat == null)
                return null;

            var (baseUomId, setupErr) = await ResolveBaseUomIdAsync(db, mat, ct);
            if (baseUomId == null)
                return setupErr;
            if (uomId == baseUomId.Value)
                return null;

            var hasConversion = await db.UnitConversions.AsNoTracking()
                .AnyAsync(c => c.MaterialNumber == key && c.AltUnitId == uomId, ct);
            if (!hasConversion)
                return UomNotAllowedMessage;
            return null;
        }

        public static async Task<(bool success, string? errorMessage, MaterialUomContextDto? data)> TryBuildMaterialUomContextAsync(
            AppDbContext db,
            string? materialNumber,
            int? includeUomIdForEdit,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(materialNumber))
                return (false, "Material is required.", null);

            var key = materialNumber.Trim();
            var mat = await db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == key, ct);
            if (mat == null)
                return (false, "Material does not exist.", null);

            var (baseUomId, baseErr) = await ResolveBaseUomIdAsync(db, mat, ct);
            if (baseUomId == null)
            {
                return (true, null, new MaterialUomContextDto
                {
                    MaterialNumber = key,
                    BaseUomId = null,
                    BaseUomCode = null,
                    SetupError = baseErr,
                    Uoms = new List<MaterialUomOptionVm>(),
                    AllowedUomIds = new List<int>()
                });
            }

            var altIds = await db.UnitConversions.AsNoTracking()
                .Where(c => c.MaterialNumber == key)
                .Select(c => c.AltUnitId)
                .Distinct()
                .ToListAsync(ct);

            var allowedBeforeExtra = new HashSet<int> { baseUomId.Value };
            foreach (var a in altIds)
                allowedBeforeExtra.Add(a);

            var idSet = new HashSet<int>(allowedBeforeExtra);
            var addedForEdit = includeUomIdForEdit is > 0 && !allowedBeforeExtra.Contains(includeUomIdForEdit.Value);
            if (addedForEdit)
                idSet.Add(includeUomIdForEdit!.Value);

            var rawUoms = await db.UnitOfMeasurements.AsNoTracking()
                .Where(u => idSet.Contains(u.Id))
                .Select(u => new { u.Id, u.Code, u.Description })
                .ToListAsync(ct);

            var uomRows = rawUoms
                .Select(u => new MaterialUomOptionVm
                {
                    Id = u.Id,
                    Code = u.Code,
                    Description = u.Description,
                    IsBase = u.Id == baseUomId.Value,
                    OutsideAllowedSet = addedForEdit && u.Id == includeUomIdForEdit!.Value
                })
                .OrderByDescending(u => u.IsBase)
                .ThenBy(u => u.Code, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var allowedIds = new List<int> { baseUomId.Value };
            allowedIds.AddRange(altIds.Where(a => !allowedIds.Contains(a)));

            return (true, null, new MaterialUomContextDto
            {
                MaterialNumber = key,
                BaseUomId = baseUomId,
                BaseUomCode = mat.BaseUnitCode?.Trim(),
                SetupError = null,
                Uoms = uomRows,
                AllowedUomIds = allowedIds
            });
        }
    }
}
