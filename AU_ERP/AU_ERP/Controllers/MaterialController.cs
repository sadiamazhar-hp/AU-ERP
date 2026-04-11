using AU_ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace AU_ERP.Main_Controller
{
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
                new SelectListItem { Text = "Chemical Industry", Value = "C" }
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMaterialMaster material, List<UnitConversion> conversions)
        {
            if (!ModelState.IsValid)
            {
                ModelState.Clear();
                PrepareViewBags();
                return View(material);
            }

            var (ok, error) = await TryPersistNewMaterialAsync(material, conversions);
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
        public async Task<IActionResult> CreateV2(CreateMaterialMaster material, List<UnitConversion> conversions)
        {
            if (!ModelState.IsValid)
            {
                var msg = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
                return Json(new { success = false, message = string.IsNullOrWhiteSpace(msg) ? "Validation failed." : msg });
            }

            var (ok, error) = await TryPersistNewMaterialAsync(material, conversions);
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
                m.PurchasingGroupCode,
                m.GrProcessingTime,
                m.MrpTypeCode,
                m.ProcurementTypeCode,
                m.StrategyGroup,
                m.AvailabilityCheckCode,
                m.ValuationClassCode
            };

            var conversions = await _context.UnitConversions.AsNoTracking()
                .Where(u => u.MaterialNumber == id)
                .OrderBy(u => u.Id)
                .Select(u => new { u.AltUnitCode, u.Numerator, u.Denominator })
                .ToListAsync();

            return Json(new { success = true, material, conversions });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMaterialV2(CreateMaterialMaster material, List<UnitConversion> conversions)
        {
            if (!ModelState.IsValid)
            {
                var msg = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
                return Json(new { success = false, message = string.IsNullOrWhiteSpace(msg) ? "Validation failed." : msg });
            }

            var original = Request.Form["OriginalMaterialNumber"].ToString();
            var (ok, error) = !string.IsNullOrWhiteSpace(original)
                && !string.Equals(original, material.MaterialNumber, StringComparison.Ordinal)
                ? await TryReplaceMaterialWithRenumberAsync(original, material, conversions)
                : await TryUpdateMaterialAsync(material, conversions);

            if (ok)
                return Json(new { success = true, message = "Material Updated Successfully !" });

            return Json(new { success = false, message = error ?? "Update failed." });
        }

        /// <summary>Delete material originally stored under <paramref name="originalMaterialNumber"/> and insert the posted row under the new <see cref="CreateMaterialMaster.MaterialNumber"/> (e.g. after type change + next number).</summary>
        private async Task<(bool Success, string? ErrorMessage)> TryReplaceMaterialWithRenumberAsync(
            string originalMaterialNumber,
            CreateMaterialMaster material,
            List<UnitConversion>? conversions)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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
                        if (string.IsNullOrEmpty(uom.AltUnitCode)) continue;

                        await _context.UnitConversions.AddAsync(new UnitConversion
                        {
                            MaterialNumber = material.MaterialNumber,
                            AltUnitCode = uom.AltUnitCode,
                            Numerator = uom.Numerator,
                            Denominator = uom.Denominator <= 0 ? 1 : uom.Denominator
                        });
                    }
                }

                if (!string.IsNullOrEmpty(material.MaterialTypeCode))
                {
                    var range = await _context.MaterialNumberRanges
                        .FirstOrDefaultAsync(x => x.MaterialTypeCode == material.MaterialTypeCode);

                    if (range != null)
                        range.CurrentNumber = material.MaterialNumber;
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

        private async Task<(bool Success, string? ErrorMessage)> TryUpdateMaterialAsync(
            CreateMaterialMaster material,
            List<UnitConversion>? conversions)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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
                existing.PurchasingGroupCode = material.PurchasingGroupCode;
                existing.GrProcessingTime = material.GrProcessingTime;
                existing.MrpTypeCode = material.MrpTypeCode;
                existing.ProcurementTypeCode = material.ProcurementTypeCode;
                existing.StrategyGroup = material.StrategyGroup;
                existing.AvailabilityCheckCode = material.AvailabilityCheckCode;
                existing.ValuationClassCode = material.ValuationClassCode;

                var oldUom = await _context.UnitConversions
                    .Where(u => u.MaterialNumber == material.MaterialNumber)
                    .ToListAsync();
                _context.UnitConversions.RemoveRange(oldUom);

                if (conversions != null && conversions.Any())
                {
                    foreach (var uom in conversions)
                    {
                        if (string.IsNullOrEmpty(uom.AltUnitCode)) continue;

                        uom.MaterialNumber = material.MaterialNumber;
                        await _context.UnitConversions.AddAsync(new UnitConversion
                        {
                            MaterialNumber = material.MaterialNumber,
                            AltUnitCode = uom.AltUnitCode,
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
            List<UnitConversion>? conversions)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.CreateMaterialMaster.AddAsync(material);

                if (conversions != null && conversions.Any())
                {
                    foreach (var uom in conversions)
                    {
                        if (string.IsNullOrEmpty(uom.AltUnitCode)) continue;

                        uom.MaterialNumber = material.MaterialNumber;
                        await _context.UnitConversions.AddAsync(uom);
                    }
                }

                if (!string.IsNullOrEmpty(material.MaterialTypeCode))
                {
                    var range = await _context.MaterialNumberRanges
                        .FirstOrDefaultAsync(x => x.MaterialTypeCode == material.MaterialTypeCode);

                    if (range != null)
                        range.CurrentNumber = material.MaterialNumber;
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
   
        [HttpGet]
        public async Task<JsonResult> GetNextMaterialNumber(string materialTypeCode)
        {
            try
            {
                var range = await _context.MaterialNumberRanges
                    .FirstOrDefaultAsync(x => x.MaterialTypeCode == materialTypeCode);

                if (range == null)
                    return Json(new { success = false, message = "No number range defined for this type." });

                long fromNum = long.Parse(range.FromNumber);
                long nextNumber = string.IsNullOrEmpty(range.CurrentNumber) || range.CurrentNumber == "0"
                    ? fromNum
                    : long.Parse(range.CurrentNumber) + 1;

                if (!string.IsNullOrEmpty(range.ToNumber))
                {
                    long toNum = long.Parse(range.ToNumber);
                    if (nextNumber > toNum)
                        return Json(new { success = false, message = "The number range for this material type has been exhausted!" });
                }

                return Json(new { success = true, nextNumber = nextNumber.ToString() });
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
            if (ranges == null || ranges.Count == 0)
                return RedirectToAction("NumberRanges");

            try
            {
                foreach (var item in ranges)
                {
                    if (string.IsNullOrEmpty(item.MaterialTypeCode)) continue;

                    var existing = await _context.MaterialNumberRanges
                        .FirstOrDefaultAsync(x => x.MaterialTypeCode == item.MaterialTypeCode);

                    if (existing != null)
                    {
                        existing.FromNumber = item.FromNumber;
                        existing.ToNumber = item.ToNumber;
                        existing.CurrentNumber = item.CurrentNumber;
                        existing.IsExternal = item.IsExternal;
                    }
                    else
                    {
                        int count = await _context.MaterialNumberRanges.CountAsync() + 1;
                        item.RangeID = count.ToString("D2");

                        while (await _context.MaterialNumberRanges.AnyAsync(x => x.RangeID == item.RangeID))
                        {
                            count++;
                            item.RangeID = count.ToString("D2");
                        }

                        await _context.MaterialNumberRanges.AddAsync(item);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Data Saved Successfully!";
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                TempData["Error"] = $"Save failed: {msg}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Save failed: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("NumberRanges");
        }

        [HttpPost]
        public async Task<JsonResult> DeleteNumberRange(string id)
        {
            var item = await _context.MaterialNumberRanges.FindAsync(id);
            if (item == null)
                return Json(new { success = false });

            _context.MaterialNumberRanges.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
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
            var item = await _context.CreateMaterialMaster.FindAsync(id);

            if (item == null)
                return Json(new { success = false, message = "Not found" });

            var uoms = await _context.UnitConversions.Where(u => u.MaterialNumber == id).ToListAsync();
            _context.UnitConversions.RemoveRange(uoms);
            _context.CreateMaterialMaster.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Material Deleted Successfully !" });
        }

        [HttpPost]
        public async Task<IActionResult> SaveMaterials([FromBody] List<CreateMaterialMaster> model)
        {
            if (model == null || model.Count == 0)
                return Json(new { success = false, message = "No data received" });

            foreach (var item in model)
            {
                var existing = await _context.CreateMaterialMaster
                    .FirstOrDefaultAsync(x => x.MaterialNumber == item.MaterialNumber);

                if (existing != null)
                {
                    // UPDATE
                    existing.Description = item.Description;
                    existing.MaterialTypeCode = item.MaterialTypeCode;
                    existing.BaseUnitCode = item.BaseUnitCode;
                }
                else
                {
                    // INSERT (IMPORTANT)
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
            if (materialTypes == null)
                return RedirectToAction(nameof(MaterialType));

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
                return RedirectToAction(nameof(MaterialType));
            }
            catch (Exception ex)
            {
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

                _context.MaterialTypes.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult MaterialGroup() => View("materialgroup");

        public IActionResult UnitOfMeasure() => View("unitofmeasure");

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