using System.Text.Json;
using AU_ERP.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Main_Controller
{
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
            _db.BPTypeNumberRanges.Where(r =>
                r.BPTypeId == bpTypeId && (r.IsActive == null || r.IsActive == true))
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
                    .Select(r => new { r.RangeID, r.BPTypeId, r.IsActive, r.StartNumber, r.EndNumber })
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

            TempData["BpSuccess"] = $"Business partner {model.BPID} saved.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult BPgroup()
        {
            return View();
        }

        public async Task<IActionResult> BPRoles(CancellationToken ct = default)
        {
            var list = await _db.BPRoles.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);
            return View("BPRoles", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BPRoles(List<BPRole>? roles, CancellationToken ct = default)
        {
            if (roles == null)
                return RedirectToAction(nameof(BPRoles));

            try
            {
                foreach (var item in roles)
                {
                    item.RoleCode = string.IsNullOrWhiteSpace(item.RoleCode) ? "" : item.RoleCode.Trim();
                    item.RoleName = item.RoleName?.Trim() ?? "";

                    if (item.Id > 0)
                    {
                        var existing = await _db.BPRoles.FindAsync(new object[] { item.Id }, ct);
                        if (existing != null)
                        {
                            if (!string.IsNullOrWhiteSpace(item.RoleCode))
                                existing.RoleCode = item.RoleCode;
                            existing.RoleName = item.RoleName;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(item.RoleCode) && string.IsNullOrWhiteSpace(item.RoleName))
                            continue;
                        if (string.IsNullOrWhiteSpace(item.RoleCode))
                            item.RoleCode = "R" + Guid.NewGuid().ToString("N")[..8];
                        await _db.BPRoles.AddAsync(new BPRole
                        {
                            RoleCode = item.RoleCode,
                            RoleName = item.RoleName,
                            BusinessPartnerMasterSamples = new List<BusinessPartnerMasterSample>()
                        }, ct);
                    }
                }
                await _db.SaveChangesAsync(ct);
                return RedirectToAction(nameof(BPRoles));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Save failed: " + ex.Message);
                return View("BPRoles", roles);
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteBPRole(int id, CancellationToken ct = default)
        {
            try
            {
                var item = await _db.BPRoles.FindAsync(new object[] { id }, ct);
                if (item == null)
                    return Json(new { success = false, message = "Record not found" });
                _db.BPRoles.Remove(item);
                await _db.SaveChangesAsync(ct);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
            if (types == null)
                return RedirectToAction(nameof(BPTypeSamples));

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
                return RedirectToAction(nameof(BPTypeSamples));
            }
            catch (Exception ex)
            {
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
                return Json(new { success = true });
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
            if (groups == null)
                return RedirectToAction(nameof(BPGroupings));

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
                return RedirectToAction(nameof(BPGroupings));
            }
            catch (Exception ex)
            {
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
                return Json(new { success = true });
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
            ViewBag.TypeList = await _db.BPTypeSamples.AsNoTracking()
                .OrderBy(t => t.Id)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Id + " — " + t.TypeName })
                .ToListAsync(ct);

            if (ranges == null)
                return RedirectToAction(nameof(BPTypeNumberRanges));

            try
            {
                foreach (var item in ranges)
                {
                    item.IsActive = item.IsActive == true;
                    item.Prefix = string.IsNullOrWhiteSpace(item.Prefix) ? null : item.Prefix.Trim();

                    if (item.RangeID > 0)
                    {
                        var existing = await _db.BPTypeNumberRanges.FindAsync(new object[] { item.RangeID }, ct);
                        if (existing != null)
                        {
                            existing.BPTypeId = item.BPTypeId;
                            existing.Prefix = item.Prefix;
                            existing.StartNumber = item.StartNumber;
                            existing.EndNumber = item.EndNumber;
                            existing.CurrentNumber = item.CurrentNumber;
                            existing.IsActive = item.IsActive;
                        }
                    }
                    else
                    {
                        if (item.StartNumber == 0 && item.EndNumber == 0 && item.CurrentNumber == 0 && (item.BPTypeId == null || item.BPTypeId == 0))
                            continue;
                        if (item.BPTypeId == 0)
                            item.BPTypeId = null;
                        await _db.BPTypeNumberRanges.AddAsync(new BPTypeNumberRanges
                        {
                            BPTypeId = item.BPTypeId,
                            Prefix = item.Prefix,
                            StartNumber = item.StartNumber,
                            EndNumber = item.EndNumber,
                            CurrentNumber = item.CurrentNumber,
                            IsActive = item.IsActive
                        }, ct);
                    }
                }
                await _db.SaveChangesAsync(ct);
                return RedirectToAction(nameof(BPTypeNumberRanges));
            }
            catch (Exception ex)
            {
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
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
