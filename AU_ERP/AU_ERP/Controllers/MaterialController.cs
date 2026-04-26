using AU_ERP.Models;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AU_ERP.Main_Controller
{
    [Authorize(Policy = "AdminDepartment")]
    public class MaterialController : Controller
    {
        private readonly AppDbContext _context;

        public MaterialController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Material
        public IActionResult Index()
        {
            return View();
        }

        // GET: Create
        public IActionResult Create()
        {
            PrepareViewBags();
            return View();
        }

        private void PrepareViewBags()
        {
            ViewBag.MaterialTypeList = _context.MaterialTypes
                .Select(m => new SelectListItem
                {
                    Value = m.MaterialTypeCode,
                    Text = m.MaterialTypeCode + " - " + m.Description
                }).ToList();

            ViewBag.IndustrySectors = new List<SelectListItem>
            {
                new SelectListItem { Text = "Mechanical Engineering", Value = "M" },
                new SelectListItem { Text = "Chemical Industry", Value = "C" },
                new SelectListItem { Text = "Manufacturing Industry", Value = "MI" }
            };

            ViewBag.UomList = _context.UnitOfMeasurements.AsNoTracking()
                .OrderBy(u => u.Code)
                .Select(u => new SelectListItem
                {
                    Value = u.Code ?? "",
                    Text = (u.Code ?? "") + " — " + (u.Description ?? "")
                })
                .Where(u => !string.IsNullOrEmpty(u.Value))
                .ToList();

            ViewBag.UomIdList = _context.UnitOfMeasurements.AsNoTracking()
                .OrderBy(u => u.Code)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = (u.Code ?? "") + " — " + (u.Description ?? "")
                })
                .ToList();

            ViewBag.MaterialGroupList = _context.MaterialGroups.AsNoTracking()
                .OrderBy(g => g.MaterialGroupCode)
                .Select(g => new SelectListItem
                {
                    Value = g.MaterialGroupCode,
                    Text = g.MaterialGroupCode + " - " + (g.Description ?? "")
                })
                .ToList();

            ViewBag.PlantList = _context.PlantsSamples.AsNoTracking()
                .OrderBy(p => p.PlantID)
                .Select(p => new SelectListItem
                {
                    Value = p.PlantID,
                    Text = p.PlantID + " — " + p.PlantName
                })
                .ToList();

            ViewBag.ProcurementTypeList = new List<SelectListItem>
            {
                new() { Text = "Internal", Value = "INTERNAL" },
                new() { Text = "External", Value = "EXTERNAL" },
                new() { Text = "Production", Value = "PRODUCTION" }
            };

            ViewBag.ItemCategoryGroupList = new List<SelectListItem>
            {
                new() { Text = "Standard", Value = "STANDARD" }
            };

            ViewBag.PurchasingGroupSelectList = new List<SelectListItem>
            {
                new() { Text = "Local", Value = "LOCAL" }
            };

            ViewBag.GrProcessingUomList = new List<SelectListItem>
            {
                new() { Text = "Hr", Value = "HR" },
                new() { Text = "Day", Value = "DAY" },
                new() { Text = "Min", Value = "MIN" }
            };
        }

        private static void HarmonizeMaterialMrpFields(CreateMaterialMaster material)
        {
            material.SafetyStock = null;
            material.ReorderPoint = null;
        }

        /// <summary>Builds per-material <see cref="UnitConversion"/> rows from <see cref="GlobalUnitConversions"/> and selected alternate UOM ids (1 base = Qty alt → Numerator=1, Denominator=Qty).</summary>
        private async Task<(List<UnitConversion> Conversions, string? ErrorMessage)> BuildUnitConversionsFromSelectionAsync(
            string? materialNumber,
            string? baseUnitCode,
            int[]? selectedAlternateUomIds,
            CancellationToken ct = default)
        {
            var list = new List<UnitConversion>();
            if (selectedAlternateUomIds == null || selectedAlternateUomIds.Length == 0)
                return (list, null);
            if (string.IsNullOrWhiteSpace(baseUnitCode))
                return (list, "Select a base unit of measure before choosing alternate units.");
            if (string.IsNullOrWhiteSpace(materialNumber))
                return (list, "Material number is required for unit conversions.");
            var baseUom = await _context.UnitOfMeasurements.AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Code != null && u.Code.Trim().ToLower() == baseUnitCode.Trim().ToLower(), ct);
            if (baseUom == null)
                return (list, "No unit of measurement matches the base unit. Add the UOM or correct the base unit.");
            var distinct = selectedAlternateUomIds.Where(x => x > 0).Distinct().ToList();
            var mn = materialNumber!.Trim();
            foreach (var altId in distinct)
            {
                if (altId == baseUom.Id)
                    continue;
                var g = await _context.GlobalUnitConversions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BaseUnitId == baseUom.Id && x.AltUnitId == altId, ct);
                if (g == null)
                {
                    var alt = await _context.UnitOfMeasurements.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == altId, ct);
                    var altLabel = alt?.Code ?? altId.ToString();
                    return (list, $"Add a unit conversion in Configuration → Unit Conversion (base to \"{altLabel}\") before selecting it as an alternate unit.");
                }
                if (g.Quantity <= 0)
                    return (list, "Invalid master conversion quantity for one of the selected alternate units.");
                var d = (float)g.Quantity;
                if (d <= 0f)
                    return (list, "Invalid master conversion quantity for one of the selected alternate units.");
                list.Add(new UnitConversion
                {
                    MaterialNumber = mn,
                    AltUnitId = altId,
                    Numerator = 1f,
                    Denominator = d
                });
            }
            return (list, null);
        }

        private static string NormalizeMaterialDescription(string? description) => (description ?? "").Trim();

        private async Task<string?> ValidateMaterialDescriptionUniqueAsync(
            string? description,
            string? excludeMaterialNumber,
            CancellationToken ct = default)
        {
            var n = NormalizeMaterialDescription(description);
            if (string.IsNullOrEmpty(n))
                return null;

            var q = _context.CreateMaterialMaster.AsQueryable();
            if (!string.IsNullOrEmpty(excludeMaterialNumber))
                q = q.Where(x => x.MaterialNumber != excludeMaterialNumber);

            if (await q.AnyAsync(x => x.Description != null && x.Description.Trim() == n, ct))
                return "Another material already uses this description.";
            return null;
        }

        private static string? ValidatePurchasingGroupLength(string? purchasingGroupCode)
        {
            if (!string.IsNullOrEmpty(purchasingGroupCode) && purchasingGroupCode.Length > 20)
                return "Purchasing group must be at most 20 characters.";
            return null;
        }

        private async Task TouchMaterialNumberRangeAfterIssueAsync(string? materialTypeCode, string materialNumber)
        {
            if (string.IsNullOrEmpty(materialTypeCode))
                return;

            var ranges = await _context.MaterialNumberRanges
                .Where(x => x.MaterialTypeCode == materialTypeCode)
                .OrderBy(x => x.RangeID)
                .ToListAsync();

            foreach (var range in ranges)
            {
                if (NumberRangeMaintenance.IsMaterialRangeExhausted(range.FromNumber, range.ToNumber, range.CurrentNumber))
                    continue;

                range.CurrentNumber = NumberRangeMaintenance.NormalizeMaterialLastIssued(
                    range.FromNumber, range.ToNumber, materialNumber);
                return;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMaterialMaster material, int[]? selectedAlternateUomIds)
        {
            HarmonizeMaterialMrpFields(material);
            if (!ModelState.IsValid)
            {
                ModelState.Clear();
                PrepareViewBags();
                return View(material);
            }

            var (ok, error) = await TryPersistNewMaterialAsync(material, selectedAlternateUomIds);
            if (ok)
            {
                TempData["SuccessMessage"] = "Material Added Successfully !";
                return RedirectToAction("Create");
            }

            ModelState.Clear();
            PrepareViewBags();
            TempData["ErrorMessage"] = error;
            return View(material);
        }

        /// <summary>GET partial for Create Material modal (V2) on list page.</summary>
        public IActionResult CreateV2()
        {
            PrepareViewBags();
            return PartialView("CreateV2", new CreateMaterialMaster());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateV2(CreateMaterialMaster material, int[]? selectedAlternateUomIds)
        {
            HarmonizeMaterialMrpFields(material);
            if (!ModelState.IsValid)
            {
                var msg = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
                return Json(new { success = false, message = string.IsNullOrWhiteSpace(msg) ? "Validation failed." : msg });
            }

            var (ok, error) = await TryPersistNewMaterialAsync(material, selectedAlternateUomIds);
            if (ok)
                return Json(new { success = true, message = "Material Added Successfully !" });

            return Json(new { success = false, message = error ?? "Save failed." });
        }

        [HttpGet]
        public async Task<JsonResult> GetMaterialForEditV2(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Missing material number." });

            var m = await _context.CreateMaterialMaster.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaterialNumber == id);

            if (m == null)
                return Json(new { success = false, message = "Not found." });

            var material = new
            {
                m.MaterialNumber,
                m.IndustrySectorCode,
                m.MaterialTypeCode,
                m.Description,
                m.BaseUnitCode,
                m.MaterialGroupCode,
                m.EAN,
                m.DeliveringPlantCode,
                m.ItemCategoryGroup,
                m.SalesPriceGradeAPerBaseUom,
                m.SalesPriceGradeBPerBaseUom,
                m.SalesPriceGradeCPerBaseUom,
                m.ScrapCostPerBaseUom,
                m.PurchasingGroupCode,
                m.GrProcessingTime,
                m.GrProcessingUom,
                m.MrpTypeCode,
                m.ProcurementTypeCode,
                m.StrategyGroup,
                m.LeadTimeDays,
                m.ValuationClassCode
            };

            var convRows = await _context.UnitConversions.AsNoTracking()
                .Where(u => u.MaterialNumber == id)
                .OrderBy(u => u.Id)
                .Select(u => new { u.AltUnitId, u.Numerator, u.Denominator })
                .ToListAsync();

            var selectedAlternateUomIds = convRows.Select(c => c.AltUnitId).ToList();

            return Json(new { success = true, material, conversions = convRows, selectedAlternateUomIds });
        }

        /// <summary>Alternates with a global conversion from the given base UOM (for material Basic Data multiselect).</summary>
        [HttpGet]
        public async Task<JsonResult> GetAlternateUomsForBaseCode(string? baseCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(baseCode))
                return Json(new { success = true, alts = Array.Empty<object>() });
            var code = baseCode.Trim();
            var baseUom = await _context.UnitOfMeasurements.AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Code != null && u.Code.Trim().ToLower() == code.ToLower(), ct);
            if (baseUom == null)
                return Json(new { success = true, alts = Array.Empty<object>() });
            var alts = await _context.GlobalUnitConversions.AsNoTracking()
                .Where(x => x.BaseUnitId == baseUom.Id)
                .Join(_context.UnitOfMeasurements,
                    g => g.AltUnitId,
                    u => u.Id,
                    (g, u) => new
                    {
                        id = g.AltUnitId,
                        code = u.Code,
                        desc = u.Description,
                        quantity = g.Quantity
                    })
                .OrderBy(x => x.code)
                .ToListAsync(ct);
            return Json(new { success = true, alts, baseUomId = baseUom.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMaterialV2(CreateMaterialMaster material, int[]? selectedAlternateUomIds)
        {
            HarmonizeMaterialMrpFields(material);
            if (!ModelState.IsValid)
            {
                var msg = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
                return Json(new { success = false, message = string.IsNullOrWhiteSpace(msg) ? "Validation failed." : msg });
            }

            var original = Request.Form["OriginalMaterialNumber"].ToString();
            var (ok, error) = !string.IsNullOrWhiteSpace(original)
                && !string.Equals(original, material.MaterialNumber, StringComparison.Ordinal)
                ? await TryReplaceMaterialWithRenumberAsync(original, material, selectedAlternateUomIds)
                : await TryUpdateMaterialAsync(material, selectedAlternateUomIds);

            if (ok)
                return Json(new { success = true, message = "Material Updated Successfully !" });

            return Json(new { success = false, message = error ?? "Update failed." });
        }

        /// <summary>Delete material originally stored under <paramref name="originalMaterialNumber"/> and insert the posted row under the new <see cref="CreateMaterialMaster.MaterialNumber"/> (e.g. after type change + next number).</summary>
        private async Task<(bool Success, string? ErrorMessage)> TryReplaceMaterialWithRenumberAsync(
            string originalMaterialNumber,
            CreateMaterialMaster material,
            int[]? selectedAlternateUomIds)
        {
            var (conversions, cErr) = await BuildUnitConversionsFromSelectionAsync(
                material.MaterialNumber, material.BaseUnitCode, selectedAlternateUomIds);
            if (cErr != null)
                return (false, cErr);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var vPur = ValidatePurchasingGroupLength(material.PurchasingGroupCode);
                if (vPur != null)
                    return (false, vPur);
                HarmonizeMaterialMrpFields(material);

                var vDesc = await ValidateMaterialDescriptionUniqueAsync(material.Description, originalMaterialNumber);
                if (vDesc != null)
                    return (false, vDesc);

                var old = await _context.CreateMaterialMaster.FindAsync(originalMaterialNumber);
                if (old == null)
                    return (false, "Original material not found.");

                if (await _context.CreateMaterialMaster.AnyAsync(x => x.MaterialNumber == material.MaterialNumber))
                    return (false, "Material number already in use.");

                var oldUom = await _context.UnitConversions
                    .Where(u => u.MaterialNumber == originalMaterialNumber)
                    .ToListAsync();
                _context.UnitConversions.RemoveRange(oldUom);
                _context.CreateMaterialMaster.Remove(old);
                await _context.SaveChangesAsync();

                await _context.CreateMaterialMaster.AddAsync(material);

                if (conversions != null && conversions.Any())
                {
                    foreach (var uom in conversions)
                    {
                        if (uom.AltUnitId <= 0) continue;

                        await _context.UnitConversions.AddAsync(new UnitConversion
                        {
                            MaterialNumber = material.MaterialNumber,
                            AltUnitId = uom.AltUnitId,
                            Numerator = uom.Numerator,
                            Denominator = uom.Denominator <= 0 ? 1 : uom.Denominator
                        });
                    }
                }

                await TouchMaterialNumberRangeAfterIssueAsync(material.MaterialTypeCode, material.MaterialNumber);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, ex.Message);
            }
        }

        private async Task<(bool Success, string? ErrorMessage)> TryUpdateMaterialAsync(
            CreateMaterialMaster material,
            int[]? selectedAlternateUomIds)
        {
            var (conversions, cErr) = await BuildUnitConversionsFromSelectionAsync(
                material.MaterialNumber, material.BaseUnitCode, selectedAlternateUomIds);
            if (cErr != null)
                return (false, cErr);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var vPur = ValidatePurchasingGroupLength(material.PurchasingGroupCode);
                if (vPur != null)
                    return (false, vPur);
                HarmonizeMaterialMrpFields(material);

                var vDesc = await ValidateMaterialDescriptionUniqueAsync(material.Description, material.MaterialNumber);
                if (vDesc != null)
                    return (false, vDesc);

                var existing = await _context.CreateMaterialMaster.FindAsync(material.MaterialNumber);
                if (existing == null)
                    return (false, "Material not found.");

                existing.IndustrySectorCode = material.IndustrySectorCode;
                existing.MaterialTypeCode = material.MaterialTypeCode;
                existing.Description = material.Description;
                existing.BaseUnitCode = material.BaseUnitCode;
                existing.MaterialGroupCode = material.MaterialGroupCode;
                existing.EAN = material.EAN;
                existing.DeliveringPlantCode = material.DeliveringPlantCode;
                existing.ItemCategoryGroup = material.ItemCategoryGroup;
                existing.SalesPriceGradeAPerBaseUom = material.SalesPriceGradeAPerBaseUom;
                existing.SalesPriceGradeBPerBaseUom = material.SalesPriceGradeBPerBaseUom;
                existing.SalesPriceGradeCPerBaseUom = material.SalesPriceGradeCPerBaseUom;
                existing.ScrapCostPerBaseUom = material.ScrapCostPerBaseUom;
                existing.PurchasingGroupCode = material.PurchasingGroupCode;
                existing.GrProcessingTime = material.GrProcessingTime;
                existing.GrProcessingUom = material.GrProcessingUom;
                existing.MrpTypeCode = material.MrpTypeCode;
                existing.ProcurementTypeCode = material.ProcurementTypeCode;
                existing.StrategyGroup = material.StrategyGroup;
                existing.LeadTimeDays = material.LeadTimeDays;
                existing.SafetyStock = null;
                existing.ReorderPoint = null;
                existing.ValuationClassCode = material.ValuationClassCode;

                var oldUom = await _context.UnitConversions
                    .Where(u => u.MaterialNumber == material.MaterialNumber)
                    .ToListAsync();
                _context.UnitConversions.RemoveRange(oldUom);

                if (conversions != null && conversions.Any())
                {
                    foreach (var uom in conversions)
                    {
                        if (uom.AltUnitId <= 0) continue;

                        await _context.UnitConversions.AddAsync(new UnitConversion
                        {
                            MaterialNumber = material.MaterialNumber,
                            AltUnitId = uom.AltUnitId,
                            Numerator = uom.Numerator,
                            Denominator = uom.Denominator <= 0 ? 1 : uom.Denominator
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, ex.Message);
            }
        }

        private async Task<(bool Success, string? ErrorMessage)> TryPersistNewMaterialAsync(
            CreateMaterialMaster material,
            int[]? selectedAlternateUomIds)
        {
            var (conversions, cErr) = await BuildUnitConversionsFromSelectionAsync(
                material.MaterialNumber, material.BaseUnitCode, selectedAlternateUomIds);
            if (cErr != null)
                return (false, cErr);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var vPur = ValidatePurchasingGroupLength(material.PurchasingGroupCode);
                if (vPur != null)
                    return (false, vPur);
                HarmonizeMaterialMrpFields(material);

                var vDesc = await ValidateMaterialDescriptionUniqueAsync(material.Description, null);
                if (vDesc != null)
                    return (false, vDesc);

                await _context.CreateMaterialMaster.AddAsync(material);

                if (conversions != null && conversions.Any())
                {
                    foreach (var uom in conversions)
                    {
                        if (uom.AltUnitId <= 0) continue;

                        uom.MaterialNumber = material.MaterialNumber;
                        await _context.UnitConversions.AddAsync(uom);
                    }
                }

                await TouchMaterialNumberRangeAfterIssueAsync(material.MaterialTypeCode, material.MaterialNumber);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, ex.Message);
            }
        }
   
        [HttpGet]
        public async Task<JsonResult> GetNextMaterialNumber(string materialTypeCode)
        {
            try
            {
                var ranges = await _context.MaterialNumberRanges
                    .Where(x => x.MaterialTypeCode == materialTypeCode)
                    .OrderBy(x => x.RangeID)
                    .ToListAsync();

                if (ranges.Count == 0)
                    return Json(new { success = false, message = "No number range defined for this type." });

                foreach (var range in ranges)
                {
                    if (NumberRangeMaintenance.IsMaterialRangeExhausted(range.FromNumber, range.ToNumber, range.CurrentNumber))
                        continue;

                    var fromNum = long.Parse(range.FromNumber);
                    var nextNumber = string.IsNullOrEmpty(range.CurrentNumber) || range.CurrentNumber == "0"
                        ? fromNum
                        : long.Parse(range.CurrentNumber) + 1;

                    if (!string.IsNullOrEmpty(range.ToNumber))
                    {
                        var toNum = long.Parse(range.ToNumber);
                        if (nextNumber > toNum)
                            continue;
                    }

                    return Json(new { success = true, nextNumber = nextNumber.ToString() });
                }

                return Json(new { success = false, message = "The number range for this material type has been exhausted!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET
        public async Task<IActionResult> NumberRanges()
        {
            ViewBag.MaterialTypes = await _context.MaterialTypes
                .Select(m => new SelectListItem
                {
                    Value = m.MaterialTypeCode,
                    Text = m.MaterialTypeCode + " - " + m.Description
                }).ToListAsync();

            var data = await _context.MaterialNumberRanges.ToListAsync();
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NumberRanges(List<MaterialNumberRange> ranges)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (ranges == null || ranges.Count == 0)
            {
                if (isAjax) return Json(new { success = false, message = "No data received." });
                return RedirectToAction("NumberRanges");
            }

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var item in ranges)
                    {
                        if (string.IsNullOrEmpty(item.MaterialTypeCode)) continue;

                        var normalizedCurrent = NumberRangeMaintenance.NormalizeMaterialLastIssued(
                            item.FromNumber, item.ToNumber, item.CurrentNumber);

                        if (item.RangeID > 0)
                        {
                            var tracked = await _context.MaterialNumberRanges.FindAsync(item.RangeID);
                            if (tracked != null)
                            {
                                tracked.MaterialTypeCode = item.MaterialTypeCode;
                                tracked.FromNumber = item.FromNumber;
                                tracked.ToNumber = item.ToNumber;
                                tracked.CurrentNumber = normalizedCurrent;
                                tracked.IsExternal = item.IsExternal;
                            }
                        }
                        else
                        {
                            var live = await _context.MaterialNumberRanges
                                .Where(x => x.MaterialTypeCode == item.MaterialTypeCode)
                                .ToListAsync();

                            if (live.Any(x => !NumberRangeMaintenance.IsMaterialRangeExhausted(
                                    x.FromNumber, x.ToNumber, x.CurrentNumber)))
                            {
                                await transaction.RollbackAsync();
                                _context.ChangeTracker.Clear();
                                const string msg =
                                    "This material type already has an active number range that is not exhausted. Add another range only after the current range is exhausted.";
                                if (isAjax) return Json(new { success = false, message = msg });
                                TempData["Error"] = msg;
                                return RedirectToAction("NumberRanges");
                            }

                            item.RangeID = 0;
                            item.CurrentNumber = normalizedCurrent;
                            await _context.MaterialNumberRanges.AddAsync(item);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch
                    {
                        // ignore double-rollback
                    }

                    _context.ChangeTracker.Clear();
                    throw;
                }

                if (isAjax) return Json(new { success = true, message = "Material ranges saved successfully !" });
                TempData["Success"] = "Data Saved Successfully!";
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (isAjax) return Json(new { success = false, message = "Save failed: " + msg });
                TempData["Error"] = $"Save failed: {msg}";
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (isAjax) return Json(new { success = false, message = "Save failed: " + msg });
                TempData["Error"] = "Save failed: " + msg;
            }

            return RedirectToAction("NumberRanges");
        }

        [HttpPost]
        public async Task<JsonResult> DeleteNumberRange(int id)
        {
            try
            {
                var item = await _context.MaterialNumberRanges.FindAsync(id);
                if (item == null)
                    return Json(new { success = false, message = "Record not found" });

                _context.MaterialNumberRanges.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Material range deleted successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        //List Page Methods
        public async Task<IActionResult> GetMaterialList()
        {
            PrepareViewBags();

            var data = await _context.CreateMaterialMaster.ToListAsync();

            ViewBag.MaterialTypes = await _context.MaterialTypes
                .Select(m => new SelectListItem
                {
                    Value = m.MaterialTypeCode,
                    Text = m.MaterialTypeCode + " - " + m.Description
                })
                .ToListAsync();

            return View(data);
        }

        [HttpPost]
        public async Task<JsonResult> DeleteMaterial(string id)
        {
            try
            {
                var item = await _context.CreateMaterialMaster.FindAsync(id);
                if (item == null)
                    return Json(new { success = false, message = "Not found" });

                var routingIds = await _context.RoutingHeadersSamples
                    .Where(r => r.MaterialNumber == id)
                    .Select(r => r.RoutingID)
                    .ToListAsync();

                if (routingIds.Count > 0)
                {
                    var productionVersions = await _context.ProductionVersions
                        .Where(p => p.RoutingId.HasValue && routingIds.Contains(p.RoutingId.Value))
                        .ToListAsync();
                    if (productionVersions.Count > 0)
                        _context.ProductionVersions.RemoveRange(productionVersions);
                }

                var routings = await _context.RoutingHeadersSamples
                    .Where(r => r.MaterialNumber == id)
                    .ToListAsync();
                if (routings.Count > 0)
                    _context.RoutingHeadersSamples.RemoveRange(routings);

                var bomItems = await _context.BomItemsSamples
                    .Where(b => b.MaterialNumber == id)
                    .ToListAsync();
                if (bomItems.Count > 0)
                    _context.BomItemsSamples.RemoveRange(bomItems);

                var uoms = await _context.UnitConversions.Where(u => u.MaterialNumber == id).ToListAsync();
                if (uoms.Count > 0)
                    _context.UnitConversions.RemoveRange(uoms);

                _context.CreateMaterialMaster.Remove(item);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Material Deleted Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveMaterials([FromBody] List<CreateMaterialMaster> model)
        {
            if (model == null || model.Count == 0)
                return Json(new { success = false, message = "No data received" });

            var nonEmptyDescs = model
                .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Description))
                .Select(i => NormalizeMaterialDescription(i.Description))
                .ToList();
            if (nonEmptyDescs.GroupBy(d => d).Any(g => g.Count() > 1))
                return Json(new { success = false, message = "Duplicate descriptions in the list are not allowed." });

            foreach (var item in model)
            {
                if (item == null) continue;

                var descErr = await ValidateMaterialDescriptionUniqueAsync(item.Description, item.MaterialNumber);
                if (descErr != null)
                    return Json(new { success = false, message = descErr });

                var existing = await _context.CreateMaterialMaster
                    .FirstOrDefaultAsync(x => x.MaterialNumber == item.MaterialNumber);

                if (existing != null)
                {
                    existing.Description = item.Description;
                    existing.MaterialTypeCode = item.MaterialTypeCode;
                    existing.BaseUnitCode = item.BaseUnitCode;
                }
                else
                {
                    _context.CreateMaterialMaster.Add(item);
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Material Updated Successfully !" });
        }

        [HttpGet]
        public async Task<IActionResult> MaterialType()
        {
            var list = await _context.MaterialTypes.ToListAsync();
            return View("materialtype", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MaterialType(List<MaterialType>? materialTypes)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (materialTypes == null || materialTypes.Count == 0)
            {
                if (isAjax) return Json(new { success = false, message = "No data received." });
                return RedirectToAction(nameof(MaterialType));
            }

            try
            {
                foreach (var item in materialTypes)
                {
                    if (string.IsNullOrEmpty(item.MaterialTypeCode)) continue;

                    var existing = await _context.MaterialTypes.FindAsync(item.MaterialTypeCode);
                    if (existing != null)
                    {
                        existing.Description = item.Description;
                        existing.FieldReference = item.FieldReference;
                    }
                    else
                    {
                        await _context.MaterialTypes.AddAsync(item);
                    }
                }

                await _context.SaveChangesAsync();
                if (isAjax) return Json(new { success = true, message = "Material Types Saved Successfully !" });
                return RedirectToAction(nameof(MaterialType));
            }
            catch (Exception ex)
            {
                if (isAjax) return Json(new { success = false, message = "Save failed: " + ex.Message });
                ModelState.AddModelError("", "Save failed: " + ex.Message);
                return View("materialtype", materialTypes);
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteMaterialType(string id)
        {
            try
            {
                var item = await _context.MaterialTypes.FindAsync(id);
                if (item == null)
                    return Json(new { success = false, message = "Record not found" });

                var materialCount = await _context.CreateMaterialMaster
                    .CountAsync(m => m.MaterialTypeCode == id);
                if (materialCount > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Cannot delete this material type: {materialCount} material record(s) still reference it. Update or remove those materials first."
                    });
                }

                var ranges = await _context.MaterialNumberRanges
                    .Where(r => r.MaterialTypeCode == id)
                    .ToListAsync();
                if (ranges.Count > 0)
                    _context.MaterialNumberRanges.RemoveRange(ranges);

                _context.MaterialTypes.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Material Type Deleted Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult MaterialGroup() => RedirectToAction("Index", "ConfigMaterialGroup");

        public IActionResult UnitOfMeasure() => RedirectToAction("Index", "UOM");

        public IActionResult SampleOfMaterialAndRangeMapping() => View("sampleofmaterialandrangemapping");

        //public async Task<IActionResult> GetMaterialList()
        //{
        //    var materials = await _context.CreateMaterialMaster
        //        .Select(m => new CreateMaterialMaster
        //        {
        //            MaterialNumber = m.MaterialNumber,
        //            Description = m.Description,
        //            MaterialTypeCode = m.MaterialTypeCode,
        //            BaseUnitCode = m.BaseUnitCode
        //        })
        //        .ToListAsync();

        //    return View(materials);
        //}

        //[HttpPost]
        //public async Task<JsonResult> Delete(string id)
        //{
        //    var item = await _context.CreateMaterialMaster.FindAsync(id);

        //    if (item == null)
        //        return Json(new { success = false });

        //    _context.CreateMaterialMaster.Remove(item);
        //    await _context.SaveChangesAsync();

        //    return Json(new { success = true });
        //}
    }

  

}