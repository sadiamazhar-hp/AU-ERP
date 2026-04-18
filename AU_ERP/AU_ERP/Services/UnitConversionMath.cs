using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;

namespace AU_ERP.Services
{
    /// <summary>
    /// Converts quantities between UOMs for one material using base UOM + <see cref="UnitConversion"/> rows.
    /// Convention: <c>quantityInBase = quantityInAlternate * (Numerator / Denominator)</c> for the alternate row (SAP-style "per 1 alt unit").
    /// </summary>
    public static class UnitConversionMath
    {
        private static decimal AltToBaseFactor(UnitConversion c)
        {
            var den = c.Denominator == 0 ? 1m : (decimal)c.Denominator;
            return (decimal)c.Numerator / den;
        }

        /// <summary>Convert a quantity from <paramref name="fromUomId"/> to base UOM for the material.</summary>
        public static async Task<(bool ok, decimal quantityBase, string? error)> ToBaseAsync(
            AppDbContext db,
            string materialNumber,
            decimal quantity,
            int fromUomId,
            CancellationToken ct = default)
        {
            var key = materialNumber.Trim();
            var mat = await db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == key, ct);
            if (mat == null)
                return (false, 0, "Material not found.");

            var (baseId, err) = await MaterialUomForMaterialHelper.ResolveBaseUomIdAsync(db, mat, ct);
            if (baseId == null)
                return (false, 0, err);

            if (fromUomId == baseId.Value)
                return (true, quantity, null);

            var conv = await db.UnitConversions.AsNoTracking()
                .FirstOrDefaultAsync(c => c.MaterialNumber == key && c.AltUnitId == fromUomId, ct);
            if (conv == null)
                return (false, 0, $"No unit conversion from the selected UOM to base for material '{key}'.");

            var f = AltToBaseFactor(conv);
            return (true, quantity * f, null);
        }

        /// <summary>Convert a quantity from base UOM to <paramref name="toUomId"/>.</summary>
        public static async Task<(bool ok, decimal quantityOut, string? error)> FromBaseAsync(
            AppDbContext db,
            string materialNumber,
            decimal quantityBase,
            int toUomId,
            CancellationToken ct = default)
        {
            var key = materialNumber.Trim();
            var mat = await db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == key, ct);
            if (mat == null)
                return (false, 0, "Material not found.");

            var (baseId, err) = await MaterialUomForMaterialHelper.ResolveBaseUomIdAsync(db, mat, ct);
            if (baseId == null)
                return (false, 0, err);

            if (toUomId == baseId.Value)
                return (true, quantityBase, null);

            var conv = await db.UnitConversions.AsNoTracking()
                .FirstOrDefaultAsync(c => c.MaterialNumber == key && c.AltUnitId == toUomId, ct);
            if (conv == null)
                return (false, 0, $"No unit conversion from base to target UOM for material '{key}'.");

            var f = AltToBaseFactor(conv);
            if (f == 0)
                return (false, 0, "Invalid conversion factor (zero).");

            return (true, quantityBase / f, null);
        }

        /// <summary>Convert <paramref name="quantity"/> from <paramref name="fromUomId"/> to <paramref name="toUomId"/>.</summary>
        public static async Task<(bool ok, decimal quantityOut, string? error)> ConvertAsync(
            AppDbContext db,
            string materialNumber,
            decimal quantity,
            int fromUomId,
            int toUomId,
            CancellationToken ct = default)
        {
            if (fromUomId == toUomId)
                return (true, quantity, null);

            var toBase = await ToBaseAsync(db, materialNumber, quantity, fromUomId, ct);
            if (!toBase.ok)
                return (false, 0, toBase.error);

            return await FromBaseAsync(db, materialNumber, toBase.quantityBase, toUomId, ct);
        }
    }
}
