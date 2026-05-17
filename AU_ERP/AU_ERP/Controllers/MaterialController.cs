using AU_ERP.Models;
using AU_ERP.Services;
using AU_ERP.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.Json;

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

        private static void HarmonizeMaterialSalesAndEanFields(CreateMaterialMaster material)
        {
            static decimal? NonNegative(decimal? v) => v is null ? null : (v.Value < 0 ? 0m : v.Value);
            if (!string.IsNullOrWhiteSpace(material.EAN))
            {
                var e = material.EAN.Trim();
                while (e.StartsWith('-')) e = e[1..].TrimStart();
                material.EAN = string.IsNullOrWhiteSpace(e) ? null : e;
            }

            material.SalesPriceGradeAPerBaseUom = NonNegative(material.SalesPriceGradeAPerBaseUom);
            material.SalesPriceGradeBPerBaseUom = NonNegative(material.SalesPriceGradeBPerBaseUom);
            material.SalesPriceGradeCPerBaseUom = NonNegative(material.SalesPriceGradeCPerBaseUom);
            material.ScrapCostPerBaseUom = NonNegative(material.ScrapCostPerBaseUom);
        }

        /// <summary>
        /// Builds per-material <see cref="UnitConversion"/> from chosen <see cref="GlobalUnitConversion"/> rows.
        /// Supports selections where material base UOM appears on either side of global definition:
        /// - Global: 1 base = qty alt, and material base == global base  => alt=global alt, factor alt->base = 1/qty.
        /// - Global: 1 base = qty alt, and material base == global alt   => alt=global base, factor alt->base = qty.
        /// </summary>
        private async Task<(List<UnitConversion> Conversions, string? ErrorMessage)> BuildUnitConversionsFromGlobalIdsAsync(
            string? materialNumber,
            string? baseUnitCode,
            int[]? selectedGlobalUnitConversionIds,
            CancellationToken ct = default)
        {
            var list = new List<UnitConversion>();
            if (selectedGlobalUnitConversionIds == null || selectedGlobalUnitConversionIds.Length == 0)
                return (list, null);
            if (string.IsNullOrWhiteSpace(baseUnitCode))
                return (list, "Select a base unit of measure before choosing alternate unit conversions.");
            if (string.IsNullOrWhiteSpace(materialNumber))
                return (list, "Material number is required for unit conversions.");
            var baseUom = await _context.UnitOfMeasurements.AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Code != null && u.Code.Trim().ToLower() == baseUnitCode.Trim().ToLower(), ct);
            if (baseUom == null)
                return (list, "No unit of measurement matches the base unit. Add the UOM or correct the base unit.");
            var distinctIds = selectedGlobalUnitConversionIds.Where(x => x > 0).Distinct().ToList();
            if (distinctIds.Count == 0)
                return (list, null);
            var globals = await _context.GlobalUnitConversions.AsNoTracking()
                .Where(g => distinctIds.Contains(g.Id))
                .ToListAsync(ct);
            if (globals.Count != distinctIds.Count)
                return (list, "One or more unit conversion definitions are missing. Refresh the page and try again (Configuration → Unit conversion).");
            var mn = materialNumber!.Trim();
            var seenPairs = new HashSet<(int BaseId, int AltId)>();
            foreach (var g in globals)
            {
                var usesBaseSide = g.BaseUnitId == baseUom.Id;
                var usesAltSide = g.AltUnitId == baseUom.Id;
                if (!usesBaseSide && !usesAltSide)
                    return (list, $"The conversion \"{g.Title}\" does not use the current base UOM. Pick another recipe or change the base unit.");
                if (g.Quantity <= 0)
                    return (list, "Invalid master conversion quantity for one of the selected definitions.");

                var derivedAltId = usesBaseSide ? g.AltUnitId : g.BaseUnitId;
                if (derivedAltId == baseUom.Id)
                    return (list, "Invalid conversion: alternate unit cannot match the base unit.");

                if (!seenPairs.Add((baseUom.Id, derivedAltId)))
                    return (list, "Select only one conversion recipe per base/alternate UOM pair (e.g. you cannot pick two different \"box → kg\" definitions).");
            }
            foreach (var g in globals)
            {
                var qty = (float)g.Quantity;
                if (qty <= 0f)
                    return (list, "Invalid master conversion quantity for one of the selected definitions.");

                var usesBaseSide = g.BaseUnitId == baseUom.Id;
                var altUnitId = usesBaseSide ? g.AltUnitId : g.BaseUnitId;
                var numerator = usesBaseSide ? 1f : qty;
                var denominator = usesBaseSide ? qty : 1f;

                list.Add(new UnitConversion
                {
                    MaterialNumber = mn,
                    AltUnitId = altUnitId,
                    Numerator = numerator,
                    Denominator = denominator,
                    GlobalUnitConversionId = g.Id
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

        private static List<string> NormalizeEnabledTabs(IEnumerable<string>? tabs)
        {
            var allowed = new HashSet<string>(AU_ERP.Models.MaterialType.AllowedTabs, StringComparer.OrdinalIgnoreCase);
            return (tabs ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Where(x => allowed.Contains(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> ParseEnabledTabs(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                return NormalizeEnabledTabs(parsed);
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<string?> ValidateMaterialTypeTabsConfiguredAsync(string? materialTypeCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(materialTypeCode))
                return "Material type is required.";

            var type = await _context.MaterialTypes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaterialTypeCode == materialTypeCode, ct);
            if (type == null)
                return "Selected material type is invalid.";

            var enabled = ParseEnabledTabs(type.EnabledTabsJson);
            if (enabled.Count == 0)
                return $"Material type {materialTypeCode} has no enabled tabs configured. Update Material Type configuration first.";

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
        public async Task<IActionResult> Create(CreateMaterialMaster material, int[]? selectedGlobalUnitConversionIds)
        {
            HarmonizeMaterialMrpFields(material);
            HarmonizeMaterialSalesAndEanFields(material);
            if (!ModelState.IsValid)
            {
                ModelState.Clear();
                PrepareViewBags();
                return View(material);
            }
            var tabCfgErr = await ValidateMaterialTypeTabsConfiguredAsync(material.MaterialTypeCode);
            if (tabCfgErr != null)
            {
                ModelState.Clear();
                PrepareViewBags();
                TempData["ErrorMessage"] = tabCfgErr;
                return View(material);
            }

            var (ok, error) = await TryPersistNewMaterialAsync(material, selectedGlobalUnitConversionIds);
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
        public async Task<IActionResult> CreateV2(CreateMaterialMaster material, int[]? selectedGlobalUnitConversionIds)
        {
            HarmonizeMaterialMrpFields(material);
            HarmonizeMaterialSalesAndEanFields(material);
            if (!ModelState.IsValid)
            {
                var msg = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
                return Json(new { success = false, message = string.IsNullOrWhiteSpace(msg) ? "Validation failed." : msg });
            }
            var tabCfgErr = await ValidateMaterialTypeTabsConfiguredAsync(material.MaterialTypeCode);
            if (tabCfgErr != null)
                return Json(new { success = false, message = tabCfgErr });

            var (ok, error) = await TryPersistNewMaterialAsync(material, selectedGlobalUnitConversionIds);
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

            var baseU = await _context.UnitOfMeasurements.AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Code != null && m.BaseUnitCode != null &&
                         u.Code.Trim().ToLower() == m.BaseUnitCode.Trim().ToLower());
            var convRows = await _context.UnitConversions.AsNoTracking()
                .Where(u => u.MaterialNumber == id)
                .OrderBy(u => u.Id)
                .ToListAsync();
            var convDtos = convRows
                .Select(c => new { c.AltUnitId, c.Numerator, c.Denominator, c.GlobalUnitConversionId })
                .ToList();
            var altIds = convRows.Select(c => c.AltUnitId).Distinct().ToList();
            var globalCandidates = baseU == null || altIds.Count == 0
                ? new List<GlobalUnitConversion>()
                : await _context.GlobalUnitConversions.AsNoTracking()
                    .Where(g =>
                        (g.BaseUnitId == baseU.Id && altIds.Contains(g.AltUnitId))
                        || (g.AltUnitId == baseU.Id && altIds.Contains(g.BaseUnitId)))
                    .ToListAsync();
            var selectedGlobalList = new List<int>();
            foreach (var c in convRows)
            {
                if (c.GlobalUnitConversionId is int gid && gid > 0)
                {
                    selectedGlobalList.Add(gid);
                    continue;
                }
                if (baseU == null) continue;
                var match = globalCandidates.FirstOrDefault(
                    g =>
                        (
                            g.BaseUnitId == baseU.Id
                            && g.AltUnitId == c.AltUnitId
                            && Math.Abs((double)g.Quantity - c.Denominator) < 0.0001
                            && Math.Abs((double)c.Numerator - 1d) < 0.0001
                        )
                        ||
                        (
                            g.AltUnitId == baseU.Id
                            && g.BaseUnitId == c.AltUnitId
                            && Math.Abs((double)g.Quantity - c.Numerator) < 0.0001
                            && Math.Abs((double)c.Denominator - 1d) < 0.0001
                        ));
                if (match != null) selectedGlobalList.Add(match.Id);
            }

            return Json(new
            {
                success = true,
                material,
                conversions = convDtos,
                selectedGlobalUnitConversionIds = selectedGlobalList
            });
        }

        /// <summary>
        /// Alternate UOMs that have at least one global conversion using the given base UOM
        /// (base can appear as global base or global alt), grouped by derived alternate UOM.
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetAlternateUomsForBaseCode(string? baseCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(baseCode))
                return Json(new { success = true, altGroups = Array.Empty<object>(), baseUomId = (int?)null });
            var code = baseCode.Trim();
            var baseUom = await _context.UnitOfMeasurements.AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Code != null && u.Code.Trim().ToLower() == code.ToLower(), ct);
            if (baseUom == null)
                return Json(new { success = true, altGroups = Array.Empty<object>(), baseUomId = (int?)null });

            var globals = await _context.GlobalUnitConversions.AsNoTracking()
                .Where(g => g.BaseUnitId == baseUom.Id || g.AltUnitId == baseUom.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            if (globals.Count == 0)
                return Json(new { success = true, altGroups = Array.Empty<object>(), baseUomId = baseUom.Id });

            var uomIds = globals.SelectMany(g => new[] { g.BaseUnitId, g.AltUnitId }).Distinct().ToList();
            var uomMap = await _context.UnitOfMeasurements.AsNoTracking()
                .Where(u => uomIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, ct)
                .ConfigureAwait(false);

            var flat = new List<(
                int Id,
                string Title,
                decimal QuantityForMaterialBase,
                int DerivedAltUnitId,
                string ChoiceLabel)>();
            foreach (var g in globals)
            {
                if (!uomMap.TryGetValue(g.BaseUnitId, out var gGlobalBase) || !uomMap.TryGetValue(g.AltUnitId, out var gGlobalAlt))
                    continue;
                var derivedAltId = g.BaseUnitId == baseUom.Id ? g.AltUnitId : g.BaseUnitId;
                // Same factor as before: how many "derived alt" units per one material base UOM.
                var qtyForBase = g.BaseUnitId == baseUom.Id
                    ? g.Quantity
                    : (g.Quantity <= 0 ? 0 : 1m / g.Quantity);
                var label = BuildAlternateUnitChoiceLabel(g, gGlobalBase, gGlobalAlt);
                flat.Add((g.Id, g.Title ?? "", qtyForBase, derivedAltId, label));
            }

            var altGroups = flat
                .GroupBy(x => x.DerivedAltUnitId)
                .Select(grp =>
                {
                    uomMap.TryGetValue(grp.Key, out var derivedUom);
                    return new
                    {
                        altUnitId = grp.Key,
                        code = derivedUom?.Code ?? "",
                        desc = derivedUom?.Description,
                        options = grp
                            .OrderBy(x => x.Title)
                            .Select(x => new { id = x.Id, title = x.Title, quantity = x.QuantityForMaterialBase, choiceLabel = x.ChoiceLabel })
                            .ToList()
                    };
                })
                .OrderBy(x => x.code)
                .ToList();
            return Json(new { success = true, altGroups, baseUomId = baseUom.Id });
        }

        /// <summary>Human label: prefer UOM description, then code.</summary>
        private static string MaterialUomReadableLabel(string? description, string? code)
        {
            var d = (description ?? "").Trim();
            if (d.Length > 0)
                return d;
            var c = (code ?? "").Trim();
            return c.Length > 0 ? c : "?";
        }

        private static string FormatDisplayedConversionQuantity(decimal q)
        {
            var rounded = decimal.Round(q, 6, MidpointRounding.AwayFromZero);
            if (rounded == decimal.Truncate(rounded))
                return decimal.ToInt64(rounded).ToString(CultureInfo.InvariantCulture);
            return rounded.ToString("0.######", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        }

        /// <summary>
        /// Shows the global recipe as defined in master data (1 global base = qty global alt), then (base) for the material form.
        /// Example: "raw conversion 2 (1 Box = 8 pieces (base))".
        /// </summary>
        private static string BuildAlternateUnitChoiceLabel(GlobalUnitConversion g, UnitOfMeasurement globalBase, UnitOfMeasurement globalAlt)
        {
            var lead = MaterialUomReadableLabel(globalBase.Description, globalBase.Code);
            var trail = MaterialUomReadableLabel(globalAlt.Description, globalAlt.Code);
            var qty = FormatDisplayedConversionQuantity(g.Quantity);
            var inner = $"1 {lead} = {qty} {trail} (base)";
            var ttl = (g.Title ?? "").Trim();
            return ttl.Length > 0 ? $"{ttl} ({inner})" : inner;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMaterialV2(CreateMaterialMaster material, int[]? selectedGlobalUnitConversionIds)
        {
            HarmonizeMaterialMrpFields(material);
            HarmonizeMaterialSalesAndEanFields(material);
            if (!ModelState.IsValid)
            {
                var msg = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
                return Json(new { success = false, message = string.IsNullOrWhiteSpace(msg) ? "Validation failed." : msg });
            }
            var tabCfgErr = await ValidateMaterialTypeTabsConfiguredAsync(material.MaterialTypeCode);
            if (tabCfgErr != null)
                return Json(new { success = false, message = tabCfgErr });

            var original = Request.Form["OriginalMaterialNumber"].ToString();
            var (ok, error) = !string.IsNullOrWhiteSpace(original)
                && !string.Equals(original, material.MaterialNumber, StringComparison.Ordinal)
                ? await TryReplaceMaterialWithRenumberAsync(original, material, selectedGlobalUnitConversionIds)
                : await TryUpdateMaterialAsync(material, selectedGlobalUnitConversionIds);

            if (ok)
                return Json(new { success = true, message = "Material Updated Successfully !" });

            return Json(new { success = false, message = error ?? "Update failed." });
        }

        /// <summary>Delete material originally stored under <paramref name="originalMaterialNumber"/> and insert the posted row under the new <see cref="CreateMaterialMaster.MaterialNumber"/> (e.g. after type change + next number).</summary>
        private async Task<(bool Success, string? ErrorMessage)> TryReplaceMaterialWithRenumberAsync(
            string originalMaterialNumber,
            CreateMaterialMaster material,
            int[]? selectedGlobalUnitConversionIds)
        {
            var (conversions, cErr) = await BuildUnitConversionsFromGlobalIdsAsync(
                material.MaterialNumber, material.BaseUnitCode, selectedGlobalUnitConversionIds);
            if (cErr != null)
                return (false, cErr);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var vPur = ValidatePurchasingGroupLength(material.PurchasingGroupCode);
                if (vPur != null)
                    return (false, vPur);
                HarmonizeMaterialMrpFields(material);
                HarmonizeMaterialSalesAndEanFields(material);

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
                            Denominator = uom.Denominator <= 0 ? 1 : uom.Denominator,
                            GlobalUnitConversionId = uom.GlobalUnitConversionId
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
            int[]? selectedGlobalUnitConversionIds)
        {
            var (conversions, cErr) = await BuildUnitConversionsFromGlobalIdsAsync(
                material.MaterialNumber, material.BaseUnitCode, selectedGlobalUnitConversionIds);
            if (cErr != null)
                return (false, cErr);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var vPur = ValidatePurchasingGroupLength(material.PurchasingGroupCode);
                if (vPur != null)
                    return (false, vPur);
                HarmonizeMaterialMrpFields(material);
                HarmonizeMaterialSalesAndEanFields(material);

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
                            Denominator = uom.Denominator <= 0 ? 1 : uom.Denominator,
                            GlobalUnitConversionId = uom.GlobalUnitConversionId
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
            int[]? selectedGlobalUnitConversionIds)
        {
            var (conversions, cErr) = await BuildUnitConversionsFromGlobalIdsAsync(
                material.MaterialNumber, material.BaseUnitCode, selectedGlobalUnitConversionIds);
            if (cErr != null)
                return (false, cErr);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var vPur = ValidatePurchasingGroupLength(material.PurchasingGroupCode);
                if (vPur != null)
                    return (false, vPur);
                HarmonizeMaterialMrpFields(material);
                HarmonizeMaterialSalesAndEanFields(material);

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

        [HttpGet]
        public async Task<JsonResult> GetEnabledTabsForMaterialType(string? materialTypeCode)
        {
            if (string.IsNullOrWhiteSpace(materialTypeCode))
                return Json(new { success = false, message = "Material type is required.", tabs = Array.Empty<string>() });

            var type = await _context.MaterialTypes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaterialTypeCode == materialTypeCode);
            if (type == null)
                return Json(new { success = false, message = "Material type not found.", tabs = Array.Empty<string>() });

            var tabs = ParseEnabledTabs(type.EnabledTabsJson);
            if (tabs.Count == 0)
                return Json(new { success = false, message = "No tabs are configured for selected material type.", tabs = Array.Empty<string>() });

            return Json(new { success = true, tabs });
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
                var existingRows = await _context.MaterialNumberRanges.AsNoTracking().ToListAsync();
                var effective = new Dictionary<int, (long From, long To)>();
                foreach (var row in existingRows)
                {
                    if (NumberRangeMaintenance.TryParseMaterialRange(row.FromNumber, row.ToNumber, out var ef, out var et, out _))
                        effective[row.RangeID] = (ef, et);
                }

                var tempId = -1;
                foreach (var item in ranges)
                {
                    if (string.IsNullOrEmpty(item.MaterialTypeCode))
                        continue;

                    if (!NumberRangeMaintenance.TryParseMaterialRange(item.FromNumber, item.ToNumber, out var fromN, out var toN, out var parseError))
                    {
                        var msg = parseError ?? "Invalid material number range.";
                        if (isAjax) return Json(new { success = false, message = msg });
                        TempData["Error"] = msg;
                        return RedirectToAction("NumberRanges");
                    }

                    var key = item.RangeID > 0 ? item.RangeID : tempId--;
                    effective[key] = (fromN, toN);
                }

                var pairs = effective.ToList();
                for (var i = 0; i < pairs.Count; i++)
                {
                    for (var j = i + 1; j < pairs.Count; j++)
                    {
                        var a = pairs[i].Value;
                        var b = pairs[j].Value;
                        if (!NumberRangeMaintenance.RangesOverlap(a.From, a.To, b.From, b.To))
                            continue;

                        var msg = $"Material ranges conflict: {a.From}-{a.To} overlaps {b.From}-{b.To}.";
                        if (isAjax) return Json(new { success = false, message = msg });
                        TempData["Error"] = msg;
                        return RedirectToAction("NumberRanges");
                    }
                }

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
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "material number range") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "material number range") });
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
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "material") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "material") });
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
                var allowed = new HashSet<string>(AU_ERP.Models.MaterialType.AllowedTabs, StringComparer.OrdinalIgnoreCase);
                foreach (var item in materialTypes)
                {
                    if (string.IsNullOrEmpty(item.MaterialTypeCode)) continue;
                    if (string.IsNullOrWhiteSpace(item.Description))
                    {
                        var msg = $"Description is required for material type {item.MaterialTypeCode}.";
                        if (isAjax) return Json(new { success = false, message = msg });
                        ModelState.AddModelError("", msg);
                        return View("materialtype", materialTypes);
                    }

                    var tabs = NormalizeEnabledTabs(item.EnabledTabs);
                    var postedTabs = item.EnabledTabs ?? new List<string>();
                    if (postedTabs.Any(x => !string.IsNullOrWhiteSpace(x) && !allowed.Contains(x)))
                    {
                        var msg = $"Material type {item.MaterialTypeCode} includes invalid tabs.";
                        if (isAjax) return Json(new { success = false, message = msg });
                        ModelState.AddModelError("", msg);
                        return View("materialtype", materialTypes);
                    }
                    if (tabs.Count == 0)
                    {
                        var msg = $"At least one tab must be enabled for material type {item.MaterialTypeCode}.";
                        if (isAjax) return Json(new { success = false, message = msg });
                        ModelState.AddModelError("", msg);
                        return View("materialtype", materialTypes);
                    }

                    item.EnabledTabs = tabs;

                    var existing = await _context.MaterialTypes.FindAsync(item.MaterialTypeCode);
                    if (existing != null)
                    {
                        existing.Description = item.Description;
                        existing.FieldReference = item.FieldReference;
                        existing.EnabledTabs = tabs;
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
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "material type") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ReferenceConstraintDeleteMessage.MapDeleteFailure(ex, "material type") });
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