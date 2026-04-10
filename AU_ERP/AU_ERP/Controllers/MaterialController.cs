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

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Add material
                await _context.CreateMaterialMaster.AddAsync(material);

                // Add conversions
                if (conversions != null && conversions.Any())
                {
                    foreach (var uom in conversions)
                    {
                        if (string.IsNullOrEmpty(uom.AltUnitCode)) continue;

                        uom.MaterialNumber = material.MaterialNumber;
                        await _context.UnitConversions.AddAsync(uom);
                    }
                }

                // Update range
                var range = await _context.MaterialNumberRanges
                    .FirstOrDefaultAsync(x => x.MaterialTypeCode == material.MrpTypeCode);

                if (range != null)
                {
                    range.CurrentNumber = material.MaterialNumber;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Material {material.MaterialNumber} created successfully!";
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                ModelState.Clear();
                PrepareViewBags();

                TempData["ErrorMessage"] = ex.Message;
                return View(material);
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
                    return Json(new { success = false, message = "No range found" });

                long fromNum = long.Parse(range.FromNumber);
                long nextNumber = string.IsNullOrEmpty(range.CurrentNumber)
                    ? fromNum
                    : long.Parse(range.CurrentNumber) + 1;

                if (!string.IsNullOrEmpty(range.ToNumber))
                {
                    long toNum = long.Parse(range.ToNumber);
                    if (nextNumber > toNum)
                        return Json(new { success = false, message = "Range exhausted" });
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
            TempData["Success"] = "Saved successfully!";
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

            _context.CreateMaterialMaster.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
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

            return Json(new { success = true });
        }
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