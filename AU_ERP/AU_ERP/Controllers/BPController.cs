using System;
using System.Linq;
using System.Text.Json;
using AU_ERP.Models;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Main_Controller
{
    [Authorize(Policy = "AdminDepartment")]
    public class BPController : Controller
    {
        private readonly AppDbContext _db;

        public BPController(AppDbContext db)
        {
            _db = db;
        }

        private async Task PrepareBpLookupListsAsync(CancellationToken ct = default)
        {
            var roles = await _db.BPRoles.AsNoTracking().OrderBy(r => r.RoleCode).ToListAsync(ct);
            var types = await _db.BPTypeSamples.AsNoTracking()
                .Where(t => t.IsActive == null || t.IsActive == true)
                .OrderBy(t => t.TypeName)
                .ToListAsync(ct);
            var groups = await _db.BPGroupings.AsNoTracking().OrderBy(g => g.GroupName).ToListAsync(ct);

            ViewBag.RoleOptions = roles;
            ViewBag.TypeOptions = types;
            ViewBag.GroupOptions = groups;
            ViewBag.RoleBasicId = roles.FirstOrDefault(r => r.RoleCode == "FLCU00")?.Id ?? 0;
            ViewBag.RoleCustomerId = roles.FirstOrDefault(r => r.RoleCode == "FLCU01")?.Id ?? 0;
            ViewBag.RoleVendorId = roles.FirstOrDefault(r => r.RoleCode == "FLVN01")?.Id ?? 0;
            ViewBag.TypeCustomerId = types.FirstOrDefault(t => string.Equals(t.TypeName, "Customer", StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
            ViewBag.TypeVendorId = types.FirstOrDefault(t => string.Equals(t.TypeName, "Vendor", StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
            ViewBag.GroupLocalId = groups.FirstOrDefault(g => string.Equals(g.GroupName, "local", StringComparison.OrdinalIgnoreCase))?.Id ?? 0;

            // Business rule: only allow these two distribution channels in BP forms.
            // Keep DB IDs, but present as OnCall / DirectSales in the UI.
            static string Norm(string? s) => (s ?? "").Replace(" ", "").Trim().ToLowerInvariant();
            var distAll = await _db.DistributionChannels.AsNoTracking().ToListAsync(ct);
            var dist = distAll
                .Where(d =>
                {
                    var n = Norm(d.DistributionChannelName);
                    return n == "oncall" || n == "directsales";
                })
                .OrderBy(d => Norm(d.DistributionChannelName) == "oncall" ? 0 : 1)
                .ToList();
            ViewBag.DistributionChannels = dist;
            ViewBag.DistributionChannelsJson = JsonSerializer.Serialize(dist.Select(d => new { id = d.DistributionChannelID, name = d.DistributionChannelName }));

            var purchRows = await _db.PurchaseSchemeRows.AsNoTracking().OrderBy(p => p.ConditionID).ToListAsync(ct);
            ViewBag.PurchaseSchemeRows = purchRows;
            ViewBag.PurchaseSchemesJson = JsonSerializer.Serialize(purchRows.Select(p => new
            {
                id = p.ConditionID,
                label = $"{p.ConditionType} — {p.ConditionSchema}"
            }));

            var salesConfigSchemas = await _db.ConfigurationSchemas.AsNoTracking()
                .Where(s => s.SchemaType == ConfigurationSchemaType.Sales)
                .OrderBy(s => s.Title)
                .ToListAsync(ct);
            var purchaseConfigSchemas = await _db.ConfigurationSchemas.AsNoTracking()
                .Where(s => s.SchemaType == ConfigurationSchemaType.Purchase)
                .OrderBy(s => s.Title)
                .ToListAsync(ct);
            ViewBag.SalesConfigurationSchemas = salesConfigSchemas;
            ViewBag.PurchaseConfigurationSchemas = purchaseConfigSchemas;
        }

        private static void NormalizePartnerFkIds(BusinessPartnerMasterSample m)
        {
            if (m.BPRoleId == 0)
                m.BPRoleId = null;
            if (m.BPTypeId == 0)
                m.BPTypeId = null;
            if (m.BPGroupingId == 0)
                m.BPGroupingId = null;
        }

        private static readonly string[] BpPaymentTermsAllowed = { "10-days", "15-days", "30-days" };
        private static readonly string[] BpPaymentMethodsAllowed = { "Cash", "Cheque", "Online" };

        private async Task ValidateBpPaymentAndSchemaFieldsAsync(BusinessPartnerMasterSample m, CancellationToken ct = default)
        {
            var pt = m.PaymentTerms?.Trim();
            if (!string.IsNullOrEmpty(pt) && Array.IndexOf(BpPaymentTermsAllowed, pt) < 0)
                ModelState.AddModelError(nameof(BusinessPartnerMasterSample.PaymentTerms), "Payment terms must be 10-days, 15-days, or 30-days.");

            var pm = m.PaymentMethods?.Trim();
            if (!string.IsNullOrEmpty(pm) && Array.IndexOf(BpPaymentMethodsAllowed, pm) < 0)
                ModelState.AddModelError(nameof(BusinessPartnerMasterSample.PaymentMethods), "Payment method must be Cash, Cheque, or Online.");

            var ss = m.SalesSchema?.Trim();
            if (!string.IsNullOrEmpty(ss))
            {
                if (!int.TryParse(ss, out var salesSchId) || salesSchId <= 0
                    || !await _db.ConfigurationSchemas.AsNoTracking()
                        .AnyAsync(s => s.Id == salesSchId && s.SchemaType == ConfigurationSchemaType.Sales, ct))
                {
                    ModelState.AddModelError(nameof(BusinessPartnerMasterSample.SalesSchema), "Select a valid sales configuration schema.");
                }
                else
                    m.SalesSchema = salesSchId.ToString();
            }

            var ps = m.PurchSchema?.Trim();
            if (!string.IsNullOrEmpty(ps))
            {
                if (!int.TryParse(ps, out var purchSchId) || purchSchId <= 0
                    || !await _db.ConfigurationSchemas.AsNoTracking()
                        .AnyAsync(s => s.Id == purchSchId && s.SchemaType == ConfigurationSchemaType.Purchase, ct))
                {
                    ModelState.AddModelError(nameof(BusinessPartnerMasterSample.PurchSchema), "Select a valid purchase configuration schema.");
                }
                else
                    m.PurchSchema = purchSchId.ToString();
            }
        }

        private async Task<string> AllocateNewBpIdAsync(CancellationToken ct = default)
        {
            var ids = await _db.BusinessPartnerMasterSamples.Select(x => x.BPID).ToListAsync(ct);
            var max = 0;
            foreach (var id in ids)
            {
                if (id.Length > 2 && id.StartsWith("BP", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(id.AsSpan(2), out var n))
                    max = Math.Max(max, n);
            }
            return $"BP{(max + 1):D6}";
        }

        private IQueryable<BPTypeNumberRanges> ActiveRangeQuery(int bpTypeId) =>
            _db.BPTypeNumberRanges.Where(r => r.BPTypeId == bpTypeId)
                .OrderBy(r => r.RangeID);

        /// <summary>Prefer active range; if none, use any range for this BP type (inactive still defines numbers).</summary>
        private async Task<BPTypeNumberRanges?> ResolveBpTypeRangeNoTrackingAsync(int bpTypeId, CancellationToken ct) =>
            await ActiveRangeQuery(bpTypeId).AsNoTracking().FirstOrDefaultAsync(ct)
            ?? await _db.BPTypeNumberRanges.AsNoTracking()
                .Where(r => r.BPTypeId == bpTypeId)
                .OrderBy(r => r.RangeID)
                .FirstOrDefaultAsync(ct);

        private async Task<BPTypeNumberRanges?> ResolveBpTypeRangeTrackedAsync(int bpTypeId, CancellationToken ct) =>
            await ActiveRangeQuery(bpTypeId).FirstOrDefaultAsync(ct)
            ?? await _db.BPTypeNumberRanges
                .Where(r => r.BPTypeId == bpTypeId)
                .OrderBy(r => r.RangeID)
                .FirstOrDefaultAsync(ct);

        private async Task ApplyRoleDerivedDefaultsAsync(BusinessPartnerMasterSample model, CancellationToken ct)
        {
            if (model.BPRoleId is not int roleId || roleId <= 0)
                return;

            var roleCode = await _db.BPRoles.AsNoTracking()
                .Where(r => r.Id == roleId)
                .Select(r => r.RoleCode)
                .FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(roleCode))
                return;

            if (roleCode == "FLCU00")
            {
                model.BPTypeId = null;
                model.BPGroupingId = null;
                return;
            }

            if (roleCode == "FLCU01")
            {
                var customerTypeId = await _db.BPTypeSamples.AsNoTracking()
                    .Where(t => (t.IsActive == null || t.IsActive == true) && t.TypeName.ToLower() == "customer")
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync(ct);
                if (customerTypeId <= 0)
                {
                    ModelState.AddModelError(nameof(BusinessPartnerMasterSample.BPTypeId), "Customer type is not configured.");
                }
                else
                {
                    model.BPTypeId = customerTypeId;
                }

                var localGroupId = await _db.BPGroupings.AsNoTracking()
                    .Where(g => g.GroupName.ToLower() == "local")
                    .Select(g => g.Id)
                    .FirstOrDefaultAsync(ct);
                if (localGroupId <= 0)
                {
                    ModelState.AddModelError(nameof(BusinessPartnerMasterSample.BPGroupingId), "Business partner group 'local' is not configured.");
                }
                else
                {
                    model.BPGroupingId = localGroupId;
                }
                return;
            }

            if (roleCode == "FLVN01")
            {
                var vendorTypeId = await _db.BPTypeSamples.AsNoTracking()
                    .Where(t => (t.IsActive == null || t.IsActive == true) && t.TypeName.ToLower() == "vendor")
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync(ct);
                if (vendorTypeId <= 0)
                {
                    ModelState.AddModelError(nameof(BusinessPartnerMasterSample.BPTypeId), "Vendor type is not configured.");
                }
                else
                {
                    model.BPTypeId = vendorTypeId;
                }
                model.BPGroupingId = null;
                return;
            }
        }

        private static bool TryComputeNextFromRange(BPTypeNumberRanges range, out int nextNumber, out string? error)
        {
            nextNumber = 0;
            error = null;
            if (range.StartNumber > range.EndNumber)
            {
                error = "Invalid number range (start greater than end).";
                return false;
            }

            if (range.CurrentNumber == 0 || range.CurrentNumber < range.StartNumber)
                nextNumber = range.StartNumber;
            else
                nextNumber = range.CurrentNumber + 1;

            if (nextNumber > range.EndNumber)
            {
                error = "The number range for this BP type has been exhausted!";
                return false;
            }

            return true;
        }

        private static string FormatBpIdFromRange(BPTypeNumberRanges range, int nextNumber) =>
            (range.Prefix ?? "") + nextNumber.ToString();

        [HttpGet]
        public async Task<JsonResult> GetNextBPID(int bpTypeId, CancellationToken ct = default)
        {
            try
            {
                if (bpTypeId <= 0)
                    return Json(new { success = false, message = "Invalid BP type." });

                var range = await ResolveBpTypeRangeNoTrackingAsync(bpTypeId, ct);
                if (range == null)
                    return Json(new { success = false, message = "No number range defined for this BP type." });

                if (!TryComputeNextFromRange(range, out var nextNum, out var err))
                    return Json(new { success = false, message = err ?? "Range error." });

                return Json(new { success = true, nextBPID = FormatBpIdFromRange(range, nextNum) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Next numeric value from BP Type Number Ranges for the given BP type id (parallel to Material GetNextMaterialNumber).</summary>
        [HttpGet]
        public async Task<JsonResult> GetNextBPTypeNumber(int bpTypeId, CancellationToken ct = default)
        {
            try
            {
                if (bpTypeId <= 0)
                    return Json(new { success = false, message = "Invalid BP type." });

                var range = await ResolveBpTypeRangeNoTrackingAsync(bpTypeId, ct);
                if (range == null)
                    return Json(new { success = false, message = "No number range defined for this BP type." });

                if (!TryComputeNextFromRange(range, out var nextNum, out var err))
                    return Json(new { success = false, message = err ?? "Range error." });

                return Json(new
                {
                    success = true,
                    nextNumber = nextNum.ToString(),
                    nextBPID = FormatBpIdFromRange(range, nextNum)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            ViewBag.FormReturnTo = "Index";
            await PrepareBpLookupListsAsync(ct);
            var partners = await _db.BusinessPartnerMasterSamples
                .AsNoTracking()
                .Include(p => p.Role)
                .Include(p => p.TypeSample)
                .Include(p => p.Grouping)
                .OrderByDescending(p => p.CreatedAt ?? DateTime.MinValue)
                .ThenBy(p => p.BPID)
                .ToListAsync(ct);
            var vm = new BPIndexViewModel
            {
                Draft = new BusinessPartnerMasterSample { BPID = "" },
                Partners = partners
            };
            return View(vm);
        }

        public async Task<IActionResult> Create(CancellationToken ct = default)
        {
            ViewBag.FormReturnTo = "Create";
            await PrepareBpLookupListsAsync(ct);
            return View(new BusinessPartnerMasterSample { BPID = "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BusinessPartnerMasterSample model, CancellationToken ct = default)
        {
            await PrepareBpLookupListsAsync(ct);
            NormalizePartnerFkIds(model);
            await ApplyRoleDerivedDefaultsAsync(model, ct);
            await ValidateBpPaymentAndSchemaFieldsAsync(model, ct);

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError(nameof(BusinessPartnerMasterSample.FullName), "Name is required.");
            }

            if (model.BPTypeId is int bpTypeForRange && bpTypeForRange > 0)
            {
                var range = await ResolveBpTypeRangeTrackedAsync(bpTypeForRange, ct);
                if (range == null)
                    ModelState.AddModelError(nameof(BusinessPartnerMasterSample.BPTypeId), "No number range defined for this BP type.");
                else if (!TryComputeNextFromRange(range, out var nextNum, out var rangeErr))
                    ModelState.AddModelError(nameof(BusinessPartnerMasterSample.BPID), rangeErr ?? "Range error.");
                else
                {
                    model.BPID = FormatBpIdFromRange(range, nextNum);
                    range.CurrentNumber = nextNum;
                }
            }
            else if (string.IsNullOrWhiteSpace(model.BPID))
            {
                model.BPID = await AllocateNewBpIdAsync(ct);
            }

            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var msg = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
                    return Json(new { success = false, message = string.IsNullOrWhiteSpace(msg) ? "Validation failed." : msg });
                }

                var ret = Request.Form["returnTo"].ToString();
                if (string.Equals(ret, "Index", StringComparison.OrdinalIgnoreCase))
                {
                    var partners = await _db.BusinessPartnerMasterSamples
                        .AsNoTracking()
                        .Include(p => p.Role)
                        .Include(p => p.TypeSample)
                        .Include(p => p.Grouping)
                        .OrderByDescending(p => p.CreatedAt ?? DateTime.MinValue)
                        .ThenBy(p => p.BPID)
                        .ToListAsync(ct);
                    ViewBag.FormReturnTo = "Index";
                    return View("Index", new BPIndexViewModel { Draft = model, Partners = partners });
                }
                ViewBag.FormReturnTo = "Create";
                return View(model);
            }

            model.CreatedAt = DateTime.UtcNow;

            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await _db.BusinessPartnerMasterSamples.AddAsync(model, ct);
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = "Partner Added Successfully !" });

            TempData["BpSuccess"] = $"Business partner {model.BPID} saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetPartnerForEdit(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Missing partner ID." });

            var p = await _db.BusinessPartnerMasterSamples.AsNoTracking()
                .FirstOrDefaultAsync(x => x.BPID == id, ct);
            if (p == null)
                return Json(new { success = false, message = "Partner not found." });

            return Json(new
            {
                success = true,
                partner = new
                {
                    p.BPID,
                    p.BPRoleId,
                    p.BPTypeId,
                    p.BPGroupingId,
                    p.FullName,
                    p.FirstName,
                    p.LastName,
                    p.CNIC,
                    p.LicenceNo,
                    p.IsActive,
                    p.Street,
                    p.HouseNo,
                    p.City,
                    p.PostalCode,
                    p.Country,
                    p.Region,
                    p.Language,
                    p.Telephone,
                    p.Mobile,
                    p.Email,
                    p.ReconAccount,
                    p.PaymentTerms,
                    p.PaymentMethods,
                    p.BankName,
                    p.AccountNumber,
                    p.DistChannel,
                    p.SalesSchema,
                    p.PurchSchema
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdatePartner(BusinessPartnerMasterSample model, CancellationToken ct = default)
        {
            NormalizePartnerFkIds(model);
            await ApplyRoleDerivedDefaultsAsync(model, ct);
            await ValidateBpPaymentAndSchemaFieldsAsync(model, ct);
            if (!ModelState.IsValid)
            {
                var msg = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
                return Json(new { success = false, message = string.IsNullOrWhiteSpace(msg) ? "Validation failed." : msg });
            }

            if (string.IsNullOrWhiteSpace(model.BPID))
                return Json(new { success = false, message = "Missing partner ID." });

            var existing = await _db.BusinessPartnerMasterSamples.FindAsync(new object[] { model.BPID }, ct);
            if (existing == null)
                return Json(new { success = false, message = "Partner not found." });

            existing.BPRoleId = model.BPRoleId;
            existing.BPTypeId = model.BPTypeId;
            existing.BPGroupingId = model.BPGroupingId;
            existing.FullName = model.FullName;
            existing.FirstName = model.FirstName;
            existing.LastName = model.LastName;
            existing.CNIC = model.CNIC;
            existing.LicenceNo = model.LicenceNo;
            existing.IsActive = model.IsActive;
            existing.Street = model.Street;
            existing.HouseNo = model.HouseNo;
            existing.City = model.City;
            existing.PostalCode = model.PostalCode;
            existing.Country = model.Country;
            existing.Region = model.Region;
            existing.Language = model.Language;
            existing.Telephone = model.Telephone;
            existing.Mobile = model.Mobile;
            existing.Email = model.Email;
            existing.ReconAccount = model.ReconAccount;
            existing.PaymentTerms = model.PaymentTerms;
            existing.PaymentMethods = model.PaymentMethods;
            existing.BankName = model.BankName;
            existing.AccountNumber = model.AccountNumber;
            existing.DistChannel = model.DistChannel;
            existing.SalesSchema = model.SalesSchema;
            existing.PurchSchema = model.PurchSchema;

            await _db.SaveChangesAsync(ct);
            return Json(new { success = true, message = "Partner Updated Successfully !" });
        }

        [HttpPost]
        public async Task<JsonResult> DeletePartner(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Missing partner ID." });

            var partner = await _db.BusinessPartnerMasterSamples.FindAsync(new object[] { id }, ct);
            if (partner == null)
                return Json(new { success = false, message = "Partner not found." });

            _db.BusinessPartnerMasterSamples.Remove(partner);
            await _db.SaveChangesAsync(ct);
            return Json(new { success = true, message = "Partner Deleted Successfully !" });
        }

        public IActionResult BPgroup()
        {
            return View();
        }

        public async Task<IActionResult> BPTypeSamples(CancellationToken ct = default)
        {
            var list = await _db.BPTypeSamples.AsNoTracking().OrderBy(t => t.Id).ToListAsync(ct);
            return View("BPTypeSamples", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BPTypeSamples(List<BPTypeSample>? types, CancellationToken ct = default)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (types == null || types.Count == 0)
            {
                if (isAjax) return Json(new { success = false, message = "No data received." });
                return RedirectToAction(nameof(BPTypeSamples));
            }

            try
            {
                foreach (var item in types)
                {
                    item.IsActive = item.IsActive == true;
                    item.TypeName = item.TypeName?.Trim() ?? "";

                    if (item.Id > 0)
                    {
                        var existing = await _db.BPTypeSamples.FindAsync(new object[] { item.Id }, ct);
                        if (existing != null)
                        {
                            existing.TypeName = item.TypeName;
                            existing.IsActive = item.IsActive;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(item.TypeName))
                            continue;
                        await _db.BPTypeSamples.AddAsync(new BPTypeSample
                        {
                            TypeName = item.TypeName,
                            IsActive = item.IsActive,
                            CreatedAt = DateTime.UtcNow,
                            BusinessPartnerMasterSamples = new List<BusinessPartnerMasterSample>(),
                            BPTypeNumberRanges = new List<BPTypeNumberRanges>()
                        }, ct);
                    }
                }
                await _db.SaveChangesAsync(ct);
                if (isAjax) return Json(new { success = true, message = "BP Types Saved Successfully !" });
                return RedirectToAction(nameof(BPTypeSamples));
            }
            catch (Exception ex)
            {
                if (isAjax) return Json(new { success = false, message = "Save failed: " + ex.Message });
                ModelState.AddModelError("", "Save failed: " + ex.Message);
                return View("BPTypeSamples", types);
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteBPTypeSample(int id, CancellationToken ct = default)
        {
            try
            {
                var item = await _db.BPTypeSamples.FindAsync(new object[] { id }, ct);
                if (item == null)
                    return Json(new { success = false, message = "Record not found" });
                _db.BPTypeSamples.Remove(item);
                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "BP Type Deleted Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> BPGroupings(CancellationToken ct = default)
        {
            var list = await _db.BPGroupings.AsNoTracking().OrderBy(g => g.Id).ToListAsync(ct);
            return View("BPGroupings", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BPGroupings(List<BPGrouping>? groups, CancellationToken ct = default)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (groups == null || groups.Count == 0)
            {
                if (isAjax) return Json(new { success = false, message = "No data received." });
                return RedirectToAction(nameof(BPGroupings));
            }

            try
            {
                foreach (var item in groups)
                {
                    item.GroupName = item.GroupName?.Trim() ?? "";

                    if (item.Id > 0)
                    {
                        var existing = await _db.BPGroupings.FindAsync(new object[] { item.Id }, ct);
                        if (existing != null)
                            existing.GroupName = item.GroupName;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(item.GroupName))
                            continue;
                        await _db.BPGroupings.AddAsync(new BPGrouping
                        {
                            GroupName = item.GroupName,
                            BusinessPartnerMasterSamples = new List<BusinessPartnerMasterSample>()
                        }, ct);
                    }
                }
                await _db.SaveChangesAsync(ct);
                if (isAjax) return Json(new { success = true, message = "BP Groupings Saved Successfully !" });
                return RedirectToAction(nameof(BPGroupings));
            }
            catch (Exception ex)
            {
                if (isAjax) return Json(new { success = false, message = "Save failed: " + ex.Message });
                ModelState.AddModelError("", "Save failed: " + ex.Message);
                return View("BPGroupings", groups);
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteBPGrouping(int id, CancellationToken ct = default)
        {
            try
            {
                var item = await _db.BPGroupings.FindAsync(new object[] { id }, ct);
                if (item == null)
                    return Json(new { success = false, message = "Record not found" });
                _db.BPGroupings.Remove(item);
                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "BP Grouping Deleted Successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> BPTypeNumberRanges(CancellationToken ct = default)
        {
            ViewBag.TypeList = await _db.BPTypeSamples.AsNoTracking()
                .OrderBy(t => t.Id)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Id + " — " + t.TypeName })
                .ToListAsync(ct);
            var list = await _db.BPTypeNumberRanges.AsNoTracking().OrderBy(r => r.RangeID).ToListAsync(ct);
            return View("BPTypeNumberRanges", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BPTypeNumberRanges(List<BPTypeNumberRanges>? ranges, CancellationToken ct = default)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            ViewBag.TypeList = await _db.BPTypeSamples.AsNoTracking()
                .OrderBy(t => t.Id)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Id + " — " + t.TypeName })
                .ToListAsync(ct);

            if (ranges == null || ranges.Count == 0)
            {
                if (isAjax) return Json(new { success = false, message = "No data received." });
                return RedirectToAction(nameof(BPTypeNumberRanges));
            }

            try
            {
                var existingRows = await _db.BPTypeNumberRanges.AsNoTracking().ToListAsync(ct);
                var effective = existingRows.ToDictionary(x => x.RangeID, x => (x.StartNumber, x.EndNumber));
                var tempId = -1;
                foreach (var item in ranges)
                {
                    var isBlankNew = item.RangeID <= 0
                        && item.StartNumber == 0
                        && item.EndNumber == 0
                        && item.CurrentNumber == 0
                        && (item.BPTypeId == null || item.BPTypeId == 0);
                    if (isBlankNew)
                        continue;

                    if (!NumberRangeMaintenance.TryValidateBpRange(item.StartNumber, item.EndNumber, out var rangeError))
                    {
                        var msg = rangeError ?? "Invalid BP range.";
                        if (isAjax) return Json(new { success = false, message = msg });
                        ModelState.AddModelError("", msg);
                        return View("BPTypeNumberRanges", ranges);
                    }

                    var key = item.RangeID > 0 ? item.RangeID : tempId--;
                    effective[key] = (item.StartNumber, item.EndNumber);
                }

                var pairs = effective.ToList();
                for (var i = 0; i < pairs.Count; i++)
                {
                    for (var j = i + 1; j < pairs.Count; j++)
                    {
                        var a = pairs[i].Value;
                        var b = pairs[j].Value;
                        if (!NumberRangeMaintenance.RangesOverlap(a.StartNumber, a.EndNumber, b.StartNumber, b.EndNumber))
                            continue;

                        var msg = $"BP ranges conflict: {a.StartNumber}-{a.EndNumber} overlaps {b.StartNumber}-{b.EndNumber}.";
                        if (isAjax) return Json(new { success = false, message = msg });
                        ModelState.AddModelError("", msg);
                        return View("BPTypeNumberRanges", ranges);
                    }
                }

                foreach (var item in ranges)
                {
                    item.Prefix = string.IsNullOrWhiteSpace(item.Prefix) ? null : item.Prefix.Trim();

                    var normalizedCurrent = NumberRangeMaintenance.NormalizeBpLastIssued(
                        item.StartNumber, item.EndNumber, item.CurrentNumber);

                    if (item.RangeID > 0)
                    {
                        var existing = await _db.BPTypeNumberRanges.FindAsync(new object[] { item.RangeID }, ct);
                        if (existing != null)
                        {
                            existing.BPTypeId = item.BPTypeId;
                            existing.Prefix = item.Prefix;
                            existing.StartNumber = item.StartNumber;
                            existing.EndNumber = item.EndNumber;
                            existing.CurrentNumber = normalizedCurrent;
                        }
                    }
                    else
                    {
                        if (item.StartNumber == 0 && item.EndNumber == 0 && normalizedCurrent == 0 && (item.BPTypeId == null || item.BPTypeId == 0))
                            continue;
                        if (item.BPTypeId == 0)
                            item.BPTypeId = null;
                        await _db.BPTypeNumberRanges.AddAsync(new BPTypeNumberRanges
                        {
                            BPTypeId = item.BPTypeId,
                            Prefix = item.Prefix,
                            StartNumber = item.StartNumber,
                            EndNumber = item.EndNumber,
                            CurrentNumber = normalizedCurrent
                        }, ct);
                    }
                }
                await _db.SaveChangesAsync(ct);
                if (isAjax) return Json(new { success = true, message = "BP ranges saved successfully !" });
                return RedirectToAction(nameof(BPTypeNumberRanges));
            }
            catch (Exception ex)
            {
                if (isAjax) return Json(new { success = false, message = "Save failed: " + ex.Message });
                ModelState.AddModelError("", "Save failed: " + ex.Message);
                return View("BPTypeNumberRanges", ranges);
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteBPTypeNumberRange(int id, CancellationToken ct = default)
        {
            try
            {
                var item = await _db.BPTypeNumberRanges.FindAsync(new object[] { id }, ct);
                if (item == null)
                    return Json(new { success = false, message = "Record not found" });
                _db.BPTypeNumberRanges.Remove(item);
                await _db.SaveChangesAsync(ct);
                return Json(new { success = true, message = "BP range deleted successfully !" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
