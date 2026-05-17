using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AU_ERP.Models;
using AU_ERP.Validation;

namespace AU_ERP.Controllers
{
    [Authorize(Policy = "AdminDepartment")]
    public class BOMController : Controller
    {
        private readonly AppDbContext _db;
        private static readonly HashSet<string> AllowedBomStatuses = new(StringComparer.OrdinalIgnoreCase) { "Active", "Inactive" };

        public BOMController(AppDbContext db) => _db = db;

        /// <summary>All planning BOM headers are FERT in simplified flow.</summary>
        private const string HeaderMaterialTypeFixed = "FERT";
        
        private static string NormalizeBomStatus(string? status)
        {
            var v = (status ?? "Active").Trim();
            if (v.Length == 0) v = "Active";
            return AllowedBomStatuses.Contains(v) ? AllowedBomStatuses.First(x => x.Equals(v, StringComparison.OrdinalIgnoreCase)) : v;
        }
        
        /// <param name="preservedValidToWhenNoneInDto">On updates, BOM end date retained when JSON does not supply Valid To (nullable column).</param>
        private async Task<string?> ValidateBomHeaderBusinessRulesAsync(BomCreateDto dto, DateTime? preservedValidToWhenNoneInDto, CancellationToken ct)
        {
            var status = NormalizeBomStatus(dto.Status);
            var plant = (dto.Plant ?? "").Trim();
            var headerMat = (dto.BomMaterialNumber ?? "").Trim();
            if (!AllowedBomStatuses.Contains(status))
                return "Invalid BOM status.";
            var rangeEndValidTo = dto.ValidTo.HasValue ? dto.ValidTo : preservedValidToWhenNoneInDto;
            if (dto.ValidFrom.HasValue && rangeEndValidTo.HasValue && dto.ValidFrom.Value.Date > rangeEndValidTo.Value.Date)
                return "Valid To date must be on or after Valid From date.";
            var headerMaterial = await _db.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialNumber == headerMat, ct);
            if (headerMaterial == null)
                return $"Header material '{headerMat}' does not exist.";
            if (!string.Equals(headerMaterial.MaterialTypeCode, HeaderMaterialTypeFixed, StringComparison.OrdinalIgnoreCase))
                return "BOM header material must be FERT.";
            return null;
        }

        /// <summary>Next numeric BOM code; first issued code is 10000, then +1 per new BOM.</summary>
        private async Task<string> AllocateNextBomCodeAsync(CancellationToken ct = default)
        {
            const long floor = 10000;
            var codes = await _db.BomHeadersSamples.AsNoTracking()
                .Where(h => h.BOMCode != null)
                .Select(h => h.BOMCode!)
                .ToListAsync(ct);

            var best = floor - 1;
            foreach (var c in codes)
            {
                if (long.TryParse(c.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var v) && v > best)
                    best = v;
            }

            return (best + 1).ToString(CultureInfo.InvariantCulture);
        }

        private async Task PrepareViewBags(CancellationToken ct = default)
        {
            ViewBag.BomLevels = new SelectList(
                await _db.BOMLevelsSamples.AsNoTracking().OrderBy(l => l.LevelID).ToListAsync(ct),
                "LevelID", "LevelName");

            var plantsForBom = await _db.PlantsSamples.AsNoTracking()
                .OrderBy(p => p.PlantID)
                .Where(p =>
                    (p.PlantName ?? "").Trim().ToLower() != "emporium"
                    && (p.PlantID ?? "").Trim().ToLower() != "emp101")
                .ToListAsync(ct);
            ViewBag.Plants = new SelectList(plantsForBom, "PlantID", "PlantName");

            ViewBag.UomList = await _db.UnitOfMeasurements.AsNoTracking()
                .OrderBy(u => u.Code)
                .ToListAsync(ct);
        }

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            await PrepareViewBags(ct);
            ViewBag.NextBomCode = await AllocateNextBomCodeAsync(ct);

            var list = await _db.BomHeadersSamples
                .AsNoTracking()
                .Include(h => h.BOMLevel)
                .Include(h => h.PlantSample)
                .Include(h => h.BomItemsSamples)
                .OrderByDescending(h => h.BomID)
                .ToListAsync(ct);

            return View(list);
        }

        [HttpGet]
        public async Task<JsonResult> GetNextBomCode(CancellationToken ct = default)
        {
            var next = await AllocateNextBomCodeAsync(ct);
            return Json(new { success = true, nextBomCode = next });
        }

        /// <summary>Materials for BOM UI: header = FERT only; component = ROH/HALB. Search q is partial match on number or description.</summary>
        [HttpGet]
        public async Task<JsonResult> SearchBomMaterials(string? purpose, string? q, string? materialType, CancellationToken ct = default)
        {
            var p = (purpose ?? "").Trim().ToLowerInvariant();
            IQueryable<CreateMaterialMaster> query = _db.CreateMaterialMaster.AsNoTracking();

            if (p == "component")
                query = query.Where(m => m.MaterialTypeCode == "ROH" || m.MaterialTypeCode == "HALB");
            else if (p == "header")
                query = query.Where(m => m.MaterialTypeCode == HeaderMaterialTypeFixed);
            else
                return Json(new { success = false, message = "Invalid purpose. Use header or component." });

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

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] BomCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                // BOM code is system-assigned (starts at 10000, increments); ignore client value on create.
                var code = await AllocateNextBomCodeAsync(ct);

                if (code.Length > 20)
                    return Json(new { success = false, message = "BOM Code must be 20 characters or less." });

                if (await _db.BomHeadersSamples.AnyAsync(h => h.BOMCode == code, ct))
                    return Json(new { success = false, message = "This BOM code is already in use." });

                var headerType = HeaderMaterialTypeFixed;
                var headerMat = dto.BomMaterialNumber?.Trim();
                if (string.IsNullOrEmpty(headerType) || string.IsNullOrEmpty(headerMat))
                    return Json(new { success = false, message = "Material Type and Material are required for the BOM header." });
                
                var hdrErr = await ValidateBomHeaderBusinessRulesAsync(dto, null, ct);
                if (hdrErr != null)
                    return Json(new { success = false, message = hdrErr });

                var header = new BomHeadersSample
                {
                    BOMCode = code,
                    BOMTitle = dto.BOMTitle?.Trim(),
                    HeaderMaterialTypeCode = headerType,
                    BomMaterialNumber = headerMat,
                    BLevel = dto.BLevel,
                    Plant = dto.Plant,
                    BaseQty = dto.BaseQty,
                    ValidFrom = dto.ValidFrom,
                    ValidTo = dto.ValidTo,
                    BomUsage = "Production",
                    AlternativeNo = code,
                    Status = NormalizeBomStatus(dto.Status),
                    IsDefaultBom = dto.IsDefaultBom
                };

                if (dto.Items != null)
                {
                    foreach (var item in dto.Items)
                    {
                        var compNum = item.MaterialNumber?.Trim();
                        if (!string.IsNullOrEmpty(compNum))
                        {
                            var compMat = await _db.CreateMaterialMaster
                                .AsNoTracking()
                                .FirstOrDefaultAsync(m => m.MaterialNumber == compNum, ct);

                            if (compMat == null)
                                return Json(new { success = false, message = $"Component material '{compNum}' does not exist." });
                            if (compMat.MaterialTypeCode is not ("ROH" or "HALB"))
                                return Json(new { success = false, message = $"Component '{compNum}' must be Raw (ROH) or Semi-finished (HALB) material." });

                            if (item.UomId is null || item.UomId <= 0)
                                return Json(new { success = false, message = "Each component line must have a UOM selected." });

                            var uomOk = await _db.UnitOfMeasurements.AsNoTracking()
                                .AnyAsync(u => u.Id == item.UomId.Value, ct);
                            if (!uomOk)
                                return Json(new { success = false, message = "One or more component UOM values are invalid." });
                        }

                        header.BomItemsSamples.Add(new BomItemsSample
                        {
                            MaterialNumber = compNum,
                            Quantity = item.Quantity,
                            UomId = string.IsNullOrEmpty(compNum) ? null : item.UomId,
                            ScrapPercentage = item.ScrapPercentage
                        });
                    }
                }

                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                if (header.IsDefaultBom)
                {
                    var others = await _db.BomHeadersSamples
                        .Where(h => h.BomMaterialNumber == header.BomMaterialNumber && h.IsDefaultBom)
                        .ToListAsync(ct);
                    foreach (var other in others)
                        other.IsDefaultBom = false;
                }
                await _db.BomHeadersSamples.AddAsync(header, ct);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return Json(new { success = true, message = "BOM Created Successfully !", bomId = header.BomID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Save failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetBomForEdit(int id, CancellationToken ct = default)
        {
            var bom = await _db.BomHeadersSamples
                .AsNoTracking()
                .Include(h => h.BomItemsSamples)
                    .ThenInclude(i => i.Uom)
                .Include(h => h.BomItemsSamples)
                    .ThenInclude(i => i.CreateMaterialMaster)
                .FirstOrDefaultAsync(h => h.BomID == id, ct);

            if (bom == null)
                return Json(new { success = false, message = "BOM not found." });

            string? headerMatDisplay = null;
            if (!string.IsNullOrEmpty(bom.BomMaterialNumber))
            {
                headerMatDisplay = await _db.CreateMaterialMaster.AsNoTracking()
                    .Where(m => m.MaterialNumber == bom.BomMaterialNumber)
                    .Select(m => m.MaterialNumber + " - " + (m.Description ?? ""))
                    .FirstOrDefaultAsync(ct);
            }

            return Json(new
            {
                success = true,
                bom = new
                {
                    bom.BomID,
                    bom.BOMCode,
                    bom.BOMTitle,
                    bom.BLevel,
                    bom.Plant,
                    bom.BaseQty,
                    bom.HeaderMaterialTypeCode,
                    bom.BomMaterialNumber,
                    bom.BomUsage,
                    bom.AlternativeNo,
                    bom.Status,
                    bom.IsDefaultBom,
                    HeaderMaterialDisplay = headerMatDisplay,
                    ValidFrom = bom.ValidFrom?.ToString("yyyy-MM-dd"),
                    ValidTo = bom.ValidTo?.ToString("yyyy-MM-dd"),
                    Items = bom.BomItemsSamples.Select(i => new
                    {
                        i.ItemID,
                        i.MaterialNumber,
                        MaterialDescription = i.CreateMaterialMaster != null ? i.CreateMaterialMaster.Description : null,
                        BaseUnitCode = i.CreateMaterialMaster != null ? i.CreateMaterialMaster.BaseUnitCode : null,
                        i.Quantity,
                        i.UomId,
                        UomCode = i.Uom != null ? i.Uom.Code : null,
                        i.ScrapPercentage
                    })
                }
            });
        }
        
        /// <remarks>Plant argument is ignored; all valid BOM variants for the material are returned regardless of BOM header plant.</remarks>
        [HttpGet]
        public async Task<JsonResult> GetBomOptionsForMrp(string? materialNumber, string? plantId, CancellationToken ct = default)
        {
            _ = plantId;
            var mat = (materialNumber ?? "").Trim();
            if (mat.Length == 0)
                return Json(new { success = true, items = Array.Empty<object>() });
            var today = DateTime.Today;
            var list = await _db.BomHeadersSamples.AsNoTracking()
                .Where(h => h.BomMaterialNumber == mat
                            && h.Status == "Active"
                            && (!h.ValidFrom.HasValue || h.ValidFrom.Value.Date <= today)
                            && (!h.ValidTo.HasValue || h.ValidTo.Value.Date >= today))
                .OrderByDescending(h => h.IsDefaultBom)
                .ThenByDescending(h => h.ValidFrom)
                .ThenByDescending(h => h.BomID)
                .Select(h => new
                {
                    bomId = h.BomID,
                    bomCode = h.BOMCode,
                    plant = h.Plant ?? "",
                    validFrom = h.ValidFrom,
                    validTo = h.ValidTo,
                    isDefault = h.IsDefaultBom
                })
                .ToListAsync(ct);
            return Json(new { success = true, items = list });
        }

        [HttpGet]
        public async Task<JsonResult> CheckExistingBaseBom(string? materialNumber, int? excludeBomId, CancellationToken ct = default)
        {
            var mat = (materialNumber ?? "").Trim();
            if (mat.Length == 0)
                return Json(new { success = true, exists = false });

            var existing = await _db.BomHeadersSamples.AsNoTracking()
                .Where(h => h.BomMaterialNumber == mat
                            && h.IsDefaultBom
                            && (!excludeBomId.HasValue || h.BomID != excludeBomId.Value))
                .OrderByDescending(h => h.BomID)
                .Select(h => new { h.BomID, h.BOMCode })
                .FirstOrDefaultAsync(ct);

            if (existing == null)
                return Json(new { success = true, exists = false });

            return Json(new
            {
                success = true,
                exists = true,
                bomId = existing.BomID,
                bomCode = existing.BOMCode ?? ("BOM-" + existing.BomID)
            });
        }

        [HttpPost]
        public async Task<JsonResult> Update([FromBody] BomCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                var header = await _db.BomHeadersSamples
                    .Include(h => h.BomItemsSamples)
                    .FirstOrDefaultAsync(h => h.BomID == dto.BomID, ct);

                if (header == null)
                    return Json(new { success = false, message = "BOM not found." });

                var code = dto.BOMCode?.Trim();
                if (string.IsNullOrEmpty(code))
                    code = await AllocateNextBomCodeAsync(ct);

                if (code.Length > 20)
                    return Json(new { success = false, message = "BOM Code must be 20 characters or less." });

                if (await _db.BomHeadersSamples.AnyAsync(h => h.BomID != dto.BomID && h.BOMCode == code, ct))
                    return Json(new { success = false, message = "This BOM code is already in use." });

                var preservedValidTo = header.ValidTo;

                // Header assembly (HALB/FERT + material number) is fixed after create so production orders can stay aligned with a stable assembly identity.

                header.BOMCode = code;
                header.BOMTitle = dto.BOMTitle?.Trim();
                header.BLevel = dto.BLevel;
                header.Plant = dto.Plant;
                header.BaseQty = dto.BaseQty;
                header.ValidFrom = dto.ValidFrom;
                // UI no longer sends Valid To; preserve existing DB value unless a caller explicitly posts a date.
                if (dto.ValidTo.HasValue)
                    header.ValidTo = dto.ValidTo;
                header.BomUsage = "Production";
                header.AlternativeNo = header.BOMCode ?? header.AlternativeNo;
                header.Status = NormalizeBomStatus(dto.Status);
                header.IsDefaultBom = dto.IsDefaultBom;
                
                var hdrErr = await ValidateBomHeaderBusinessRulesAsync(dto, preservedValidTo, ct);
                if (hdrErr != null)
                    return Json(new { success = false, message = hdrErr });

                _db.BomItemsSamples.RemoveRange(header.BomItemsSamples);

                if (dto.Items != null)
                {
                    foreach (var item in dto.Items)
                    {
                        var compNum = item.MaterialNumber?.Trim();
                        if (!string.IsNullOrEmpty(compNum))
                        {
                            var compMat = await _db.CreateMaterialMaster
                                .AsNoTracking()
                                .FirstOrDefaultAsync(m => m.MaterialNumber == compNum, ct);

                            if (compMat == null)
                                return Json(new { success = false, message = $"Component material '{compNum}' does not exist." });
                            if (compMat.MaterialTypeCode is not ("ROH" or "HALB"))
                                return Json(new { success = false, message = $"Component '{compNum}' must be Raw (ROH) or Semi-finished (HALB) material." });

                            if (item.UomId is null || item.UomId <= 0)
                                return Json(new { success = false, message = "Each component line must have a UOM selected." });

                            var uomOk = await _db.UnitOfMeasurements.AsNoTracking()
                                .AnyAsync(u => u.Id == item.UomId.Value, ct);
                            if (!uomOk)
                                return Json(new { success = false, message = "One or more component UOM values are invalid." });
                        }

                        header.BomItemsSamples.Add(new BomItemsSample
                        {
                            MaterialNumber = compNum,
                            Quantity = item.Quantity,
                            UomId = string.IsNullOrEmpty(compNum) ? null : item.UomId,
                            ScrapPercentage = item.ScrapPercentage
                        });
                    }
                }

                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                if (header.IsDefaultBom)
                {
                    var others = await _db.BomHeadersSamples
                        .Where(h => h.BomID != header.BomID
                                    && h.BomMaterialNumber == header.BomMaterialNumber
                                    && h.IsDefaultBom)
                        .ToListAsync(ct);
                    foreach (var other in others)
                        other.IsDefaultBom = false;
                }
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return Json(new { success = true, message = "BOM Updated Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Update failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        /// <summary>Returns a user-facing message when the BOM is referenced elsewhere, or null when safe to delete.</summary>
        private async Task<string?> GetBomDeletionBlockReasonAsync(int bomId, CancellationToken ct)
        {
            var areas = new List<string>();

            if (await _db.GoodsIssueDocumentLines.AsNoTracking().AnyAsync(l => l.SelectedBomId == bomId, ct))
                areas.Add("goods issue documents");

            if (await _db.ProductionOrders.AsNoTracking().AnyAsync(o => o.SelectedBomId == bomId, ct))
                areas.Add("production orders");

            if (await _db.ProductionOrderLines.AsNoTracking().AnyAsync(l => l.SelectedBomId == bomId, ct))
                areas.Add("production order lines");

            if (await _db.ProductionVersions.AsNoTracking().AnyAsync(v => v.BomId == bomId, ct))
                areas.Add("production versions");

            if (await _db.BomHeadersSamples.AsNoTracking().AnyAsync(h => h.AlternativeBOM == bomId, ct))
                areas.Add("other BOMs (alternative)");

            if (areas.Count == 0)
                return null;

            return "This bill of material cannot be deleted because it is still in use: "
                + string.Join(", ", areas)
                + ". Remove or reassign those references, then try again.";
        }

        private static string MapBomDeleteFailureMessage(Exception ex)
        {
            if (ReferenceConstraintDeleteMessage.IsReferenceConstraint(ex))
            {
                return "This bill of material cannot be deleted because it is still referenced elsewhere "
                    + "(for example goods issues, production, or related records). Remove or reassign those references, then try again.";
            }

            return ex.InnerException?.Message ?? ex.Message;
        }

        [HttpPost]
        public async Task<JsonResult> Delete(int id, CancellationToken ct = default)
        {
            try
            {
                var blocked = await GetBomDeletionBlockReasonAsync(id, ct);
                if (blocked != null)
                    return Json(new { success = false, message = blocked });

                var header = await _db.BomHeadersSamples
                    .Include(h => h.BomItemsSamples)
                    .FirstOrDefaultAsync(h => h.BomID == id, ct);

                if (header == null)
                    return Json(new { success = false, message = "BOM not found." });

                _db.BomItemsSamples.RemoveRange(header.BomItemsSamples);
                _db.BomHeadersSamples.Remove(header);
                await _db.SaveChangesAsync(ct);

                return Json(new { success = true, message = "BOM Deleted Successfully !" });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = MapBomDeleteFailureMessage(ex) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = MapBomDeleteFailureMessage(ex) });
            }
        }
    }

    public class BomCreateDto
    {
        public int BomID { get; set; }
        public string? BOMCode { get; set; }
        public string? BOMTitle { get; set; }
        /// <summary>HALB or FERT.</summary>
        public string? HeaderMaterialTypeCode { get; set; }
        /// <summary>Header (assembly) material number.</summary>
        public string? BomMaterialNumber { get; set; }
        public int? BLevel { get; set; }
        public string? Plant { get; set; }
        public decimal? BaseQty { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? Status { get; set; }
        public bool IsDefaultBom { get; set; }
        public List<BomItemDto>? Items { get; set; }
    }

    public class BomItemDto
    {
        public string? MaterialNumber { get; set; }
        public decimal? Quantity { get; set; }
        public int? UomId { get; set; }
        public decimal? ScrapPercentage { get; set; }
    }
}
