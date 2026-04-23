using System;
using System.Linq;
using System.Text.Json;
using AU_ERP.Models;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Main_Controller
{
    [Authorize(Policy = "AdminDepartment")]
    public class BPController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public BPController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // #region agent log
        private void AgentDebugNdjson(string hypothesisId, string location, string message, object? data)
        {
            try
            {
                var path = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "..", "debug-9c40b7.log"));
                var payload = new
                {
                    sessionId = "9c40b7",
                    hypothesisId,
                    location,
                    message,
                    data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                System.IO.File.AppendAllText(path, JsonSerializer.Serialize(payload) + "\n");
            }
            catch
            {
                /* ignore debug I/O errors */
            }
        }
        // #endregion

        private async Task PrepareBpLookupListsAsync(CancellationToken ct = default)
        {
            ViewBag.RoleOptions = await _db.BPRoles.AsNoTracking().OrderBy(r => r.RoleCode).ToListAsync(ct);
            ViewBag.TypeOptions = await _db.BPTypeSamples.AsNoTracking()
                .Where(t => t.IsActive == null || t.IsActive == true)
                .OrderBy(t => t.TypeName)
                .ToListAsync(ct);
            ViewBag.GroupOptions = await _db.BPGroupings.AsNoTracking().OrderBy(g => g.GroupName).ToListAsync(ct);

            var dist = await _db.DistributionChannels.AsNoTracking().OrderBy(d => d.DistributionChannelID).ToListAsync(ct);
            ViewBag.DistributionChannels = dist;
            ViewBag.DistributionChannelsJson = JsonSerializer.Serialize(dist.Select(d => new { id = d.DistributionChannelID, name = d.DistributionChannelName }));

            var purchRows = await _db.PurchaseSchemeRows.AsNoTracking().OrderBy(p => p.ConditionID).ToListAsync(ct);
            ViewBag.PurchaseSchemeRows = purchRows;
            ViewBag.PurchaseSchemesJson = JsonSerializer.Serialize(purchRows.Select(p => new
            {
                id = p.ConditionID,
                label = $"{p.ConditionType} — {p.ConditionSchema}"
            }));
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

        private void ValidateBpPaymentAndSalesFields(BusinessPartnerMasterSample m)
        {
            var pt = m.PaymentTerms?.Trim();
            if (!string.IsNullOrEmpty(pt) && Array.IndexOf(BpPaymentTermsAllowed, pt) < 0)
                ModelState.AddModelError(nameof(BusinessPartnerMasterSample.PaymentTerms), "Payment terms must be 10-days, 15-days, or 30-days.");

            var pm = m.PaymentMethods?.Trim();
            if (!string.IsNullOrEmpty(pm) && Array.IndexOf(BpPaymentMethodsAllowed, pm) < 0)
                ModelState.AddModelError(nameof(BusinessPartnerMasterSample.PaymentMethods), "Payment method must be Cash, Cheque, or Online.");

            var ss = m.SalesSchema?.Trim();
            if (string.IsNullOrEmpty(ss))
                return;
            if (string.Equals(ss, "Walk-in", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ss, "Walk-In", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ss, "WALKIN", StringComparison.OrdinalIgnoreCase))
            {
                m.SalesSchema = "Walk-in";
                return;
            }
            if (string.Equals(ss, "Dealer", StringComparison.OrdinalIgnoreCase))
            {
                m.SalesSchema = "Dealer";
                return;
            }
            ModelState.AddModelError(nameof(BusinessPartnerMasterSample.SalesSchema), "Sales schema must be Walk-in or Dealer.");
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

                // #region agent log
                var cntByTypeId = await _db.BPTypeNumberRanges.AsNoTracking().CountAsync(r => r.BPTypeId == bpTypeId, ct);
                var cntActiveFilter = await ActiveRangeQuery(bpTypeId).AsNoTracking().CountAsync(ct);
                var probe = await _db.BPTypeNumberRanges.AsNoTracking()
                    .Where(r => r.BPTypeId == bpTypeId)
                    .Select(r => new { r.RangeID, r.BPTypeId, r.StartNumber, r.EndNumber })
                    .FirstOrDefaultAsync(ct);
                AgentDebugNdjson("H1-H5", "BPController.GetNextBPTypeNumber:beforeActiveQuery", "BPTypeNumberRanges probe",
                    new { bpTypeId, cntByTypeId, cntActiveFilter, probe, contentRootPath = _env.ContentRootPath });
                // #endregion

                var range = await ResolveBpTypeRangeNoTrackingAsync(bpTypeId, ct);
                // #region agent log
                AgentDebugNdjson("post-fix", "BPController.GetNextBPTypeNumber:afterResolve", "resolved range",
                    new { bpTypeId, rangeFound = range != null, usedInactiveFallback = range != null && cntActiveFilter == 0 && cntByTypeId > 0, rangeId = range?.RangeID });
                // #endregion
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
            ValidateBpPaymentAndSalesFields(model);

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
            ValidateBpPaymentAndSalesFields(model);
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
