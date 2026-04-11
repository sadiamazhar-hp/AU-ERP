using AU_ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Main_Controller
{
    public class BPController : Controller
    {
        private readonly AppDbContext _db;

        public BPController(AppDbContext db)
        {
            _db = db;
        }

        private void PrepareBpLookupLists()
        {
            ViewBag.RoleList = new SelectList(_db.BPRoles.AsNoTracking().OrderBy(r => r.RoleID), "RoleID", "RoleName");
            ViewBag.TypeList = new SelectList(_db.BPTypeSamples.AsNoTracking().OrderBy(t => t.BPTypeID), "BPTypeID", "TypeName");
            ViewBag.GroupList = new SelectList(_db.BPGroupings.AsNoTracking().OrderBy(g => g.GroupID), "GroupID", "GroupName");
        }

        private static void NormalizeNullableKeys(BusinessPartnerMasterSample m)
        {
            m.BPRole = string.IsNullOrWhiteSpace(m.BPRole) ? null : m.BPRole.Trim();
            m.BPType = string.IsNullOrWhiteSpace(m.BPType) ? null : m.BPType.Trim();
            m.BPGrouping = string.IsNullOrWhiteSpace(m.BPGrouping) ? null : m.BPGrouping.Trim();
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

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            ViewBag.FormReturnTo = "Index";
            PrepareBpLookupLists();
            var partners = await _db.BusinessPartnerMasterSamples
                .AsNoTracking()
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

        public IActionResult Create()
        {
            ViewBag.FormReturnTo = "Create";
            PrepareBpLookupLists();
            return View(new BusinessPartnerMasterSample { BPID = "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BusinessPartnerMasterSample model, CancellationToken ct = default)
        {
            PrepareBpLookupLists();
            NormalizeNullableKeys(model);

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError(nameof(BusinessPartnerMasterSample.FullName), "Name is required.");
            }

            if (!ModelState.IsValid)
            {
                var ret = Request.Form["returnTo"].ToString();
                if (string.Equals(ret, "Index", StringComparison.OrdinalIgnoreCase))
                {
                    var partners = await _db.BusinessPartnerMasterSamples
                        .AsNoTracking()
                        .OrderByDescending(p => p.CreatedAt ?? DateTime.MinValue)
                        .ThenBy(p => p.BPID)
                        .ToListAsync(ct);
                    ViewBag.FormReturnTo = "Index";
                    return View("Index", new BPIndexViewModel { Draft = model, Partners = partners });
                }
                ViewBag.FormReturnTo = "Create";
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.BPID))
                model.BPID = await AllocateNewBpIdAsync(ct);

            model.CreatedAt = DateTime.UtcNow;
            model.BPNumberRanges = new List<BPNumberRanges>();

            await _db.BusinessPartnerMasterSamples.AddAsync(model, ct);
            await _db.SaveChangesAsync(ct);

            TempData["BpSuccess"] = $"Business partner {model.BPID} saved.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult BPgroup()
        {
            return View();
        }
    }
}
