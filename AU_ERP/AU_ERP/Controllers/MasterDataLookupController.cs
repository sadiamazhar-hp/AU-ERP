using AU_ERP.Models;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers
{
    /// <summary>
    /// Read-only master data lookups for transactional modules (any authenticated user).
    /// Admin CRUD screens remain on AdminDepartment controllers.
    ///
    /// Registry:
    /// - Materials: SearchBomMaterials (HALB/FERT/ROH search for BOM, routing, PO forms)
    /// - Bill of materials: GetBomOptionsForMrp (MRP BOM dropdown)
    /// - Materials (FERT search/UOM): SalesQuotation, SalesOrder, MRP, ProductionOrder module endpoints
    /// - Business partners: SalesQuotation, SalesOrder module endpoints
    /// - Drivers/vehicles: DeliveryChallanController server-side lists
    /// </summary>
    [Authorize]
    public class MasterDataLookupController : Controller
    {
        private readonly AppDbContext _db;

        public MasterDataLookupController(AppDbContext db) => _db = db;

        /// <summary>
        /// Shared material lookup for dropdowns across modules.
        /// purpose=header: HALB/FERT (optionally narrowed by materialType=HALB|FERT)
        /// purpose=component: ROH/HALB/PACK
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> SearchBomMaterials(string? purpose, string? q, string? materialType, CancellationToken ct = default)
        {
            var p = (purpose ?? "").Trim().ToLowerInvariant();
            IQueryable<CreateMaterialMaster> query = _db.CreateMaterialMaster.AsNoTracking();

            if (p == "component")
                query = query.Where(m =>
                    m.MaterialTypeCode == "ROH"
                    || m.MaterialTypeCode == "HALB"
                    || m.MaterialTypeCode == "PACK");
            else if (p == "header")
            {
                var mt = (materialType ?? "").Trim().ToUpperInvariant();
                if (mt is "HALB" or "FERT")
                    query = query.Where(m => m.MaterialTypeCode == mt);
                else
                    query = query.Where(m => m.MaterialTypeCode == "HALB" || m.MaterialTypeCode == "FERT");
            }
            else
            {
                return Json(new { success = false, message = "Invalid purpose. Use header or component." });
            }

            var qq = (q ?? "").Trim();
            if (qq.Length > 0)
                query = query.Where(m =>
                    m.MaterialNumber.Contains(qq) ||
                    (m.Description != null && m.Description.Contains(qq)));

            var mats = await query
                .OrderBy(m => m.MaterialNumber)
                .Take(50)
                .Select(m => new
                {
                    m.MaterialNumber,
                    Description = m.Description ?? "",
                    m.MaterialTypeCode,
                    m.BaseUnitCode
                })
                .ToListAsync(ct);

            var codes = mats
                .Where(m => !string.IsNullOrEmpty(m.BaseUnitCode))
                .Select(m => m.BaseUnitCode!)
                .Distinct()
                .ToList();

            var uomMap = await _db.UnitOfMeasurements.AsNoTracking()
                .Where(u => u.Code != null && codes.Contains(u.Code))
                .ToDictionaryAsync(u => u.Code!, u => u.Id, ct);

            var items = mats.Select(m => new
            {
                n = m.MaterialNumber,
                d = m.Description,
                t = m.MaterialTypeCode,
                baseUom = m.BaseUnitCode ?? "",
                uomId = m.BaseUnitCode != null && uomMap.TryGetValue(m.BaseUnitCode, out var uid) ? (int?)uid : null
            }).ToList();

            return Json(new { success = true, items });
        }

        /// <remarks>Plant argument is ignored; all valid BOM variants for the material are returned regardless of BOM header plant.</remarks>
        [HttpGet]
        public async Task<JsonResult> GetBomOptionsForMrp(string? materialNumber, string? plantId, CancellationToken ct = default)
        {
            _ = plantId;
            var mat = (materialNumber ?? "").Trim();
            if (mat.Length == 0)
                return Json(new { success = true, items = Array.Empty<object>() });

            var list = await BomMrpLookup.GetOptionsAsync(_db, mat, ct: ct);
            var items = list.Select(h => new
            {
                bomId = h.BomId,
                bomCode = h.BomCode,
                plant = h.Plant,
                validFrom = h.ValidFrom,
                isDefault = h.IsDefaultBom
            }).ToList();

            return Json(new { success = true, items });
        }
    }
}
