using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

public sealed class DashboardDataService
{
    private readonly AppDbContext _db;

    public DashboardDataService(AppDbContext db) => _db = db;

    public async Task<DashboardPageVm> BuildAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var display = user.Identity?.Name?.Trim();
        if (string.IsNullOrEmpty(display) && user.Identity is ClaimsIdentity id)
        {
            display = id.FindFirst(ClaimTypes.Email)?.Value
                ?? id.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        if (string.IsNullOrEmpty(display)) display = "User";

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        IReadOnlyList<string> deptNames;
        IReadOnlyList<string> deptCodes;
        if (!string.IsNullOrEmpty(userId))
        {
            var rows = await _db.ApplicationUserDepartments
                .AsNoTracking()
                .Where(ud => ud.UserId == userId)
                .Select(ud => new { ud.Department.Code, ud.Department.Name })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            deptCodes = rows.Select(x => x.Code).OrderBy(c => c).ToList();
            deptNames = rows.Select(x => x.Name).OrderBy(n => n).ToList();
        }
        else
        {
            deptNames = Array.Empty<string>();
            deptCodes = Array.Empty<string>();
        }

        var showAd = user.HasClaim(AuClaimTypes.Department, "Admin");
        var showSt = user.HasClaim(AuClaimTypes.Department, "Store");
        var showSa = user.HasClaim(AuClaimTypes.Department, "Sales");
        var showPr = user.HasClaim(AuClaimTypes.Department, "Production");
        var hasAny = showAd || showSt || showSa || showPr;

        AdminModuleStats? admin = null;
        if (showAd)
        {
            var mat = await _db.CreateMaterialMaster.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
            var bp = await _db.BusinessPartnerMasterSamples.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
            var bom = await _db.BomHeadersSamples.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
            var wc = await _db.WorkCenterMasterSamples.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
            var rt = await _db.RoutingHeadersSamples.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
            var ucnt = await _db.Users.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
            var byType = await _db.CreateMaterialMaster.AsNoTracking()
                .GroupBy(m => m.MaterialTypeCode ?? "—")
                .Select(g => new { g.Key, C = g.Count() })
                .OrderByDescending(x => x.C)
                .Take(6)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var matByType = byType
                .Select(x => new LabelCountDto
                {
                    Label = x.Key,
                    Count = x.C,
                    Value = x.C
                })
                .ToList();
            admin = new AdminModuleStats
            {
                MaterialCount = mat,
                BusinessPartnerCount = bp,
                BomCount = bom,
                WorkCentreCount = wc,
                RoutingCount = rt,
                UserCount = ucnt,
                BarLabels = new[] { "Materials", "Business partners", "BOMs", "Work centres", "Routings", "Users" },
                BarValues = new[] { mat, bp, bom, wc, rt, ucnt },
                MaterialByType = matByType
            };
        }

        StoreModuleStats? store = null;
        if (showSt)
        {
            var storePlant = user.FindFirst(AuClaimTypes.StorePlant)?.Value?.Trim();

            List<StockInventoryLine> lines;
            if (string.IsNullOrEmpty(storePlant))
            {
                lines = new List<StockInventoryLine>();
            }
            else
            {
                lines = await _db.StockInventoryLines.AsNoTracking()
                    .Where(s => s.Status == StockInventoryLine.StatusActive && s.PlantID == storePlant)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
            }

            var byGrade = lines
                .GroupBy(s => string.IsNullOrEmpty(s.Grade) ? "—" : s.Grade)
                .Select(g => new LabelCountDto
                {
                    Label = g.Key,
                    Count = g.Count(),
                    Value = g.Sum(x => x.StockValue)
                })
                .OrderByDescending(x => x.Value)
                .ToList();
            store = new StoreModuleStats
            {
                ActiveStockLineCount = lines.Count,
                DistinctMaterialCount = lines.Select(s => s.MaterialNumber).Distinct().Count(),
                TotalStockValue = lines.Sum(s => s.StockValue),
                ValueByGrade = byGrade
            };
        }

        SalesModuleStats? sales = null;
        if (showSa)
        {
            var qD = await _db.SalesQuotations.AsNoTracking()
                .CountAsync(x => x.Status == SalesQuotation.StatusDraft, ct).ConfigureAwait(false);
            var qS = await _db.SalesQuotations.AsNoTracking()
                .CountAsync(x => x.Status == SalesQuotation.StatusSent, ct).ConfigureAwait(false);
            var oO = await _db.SalesOrders.AsNoTracking()
                .CountAsync(x => x.Status == SalesOrder.StatusOpen, ct).ConfigureAwait(false);
            var oC = await _db.SalesOrders.AsNoTracking()
                .CountAsync(x => x.Status == SalesOrder.StatusConfirmed, ct).ConfigureAwait(false);
            var dc = await _db.DeliveryChallans.AsNoTracking().CountAsync(ct).ConfigureAwait(false);

            var from = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(-5);
            var quots = await _db.SalesQuotations.AsNoTracking()
                .Where(x => x.QuotationDate >= from.Date)
                .Select(x => new { x.QuotationDate, x.QuotationNumber })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var orders = await _db.SalesOrders.AsNoTracking()
                .Where(x => x.OrderDate >= from.Date)
                .Select(x => new { x.OrderDate })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var qm = new List<LabelCountDto>();
            var om = new List<LabelCountDto>();
            for (var i = 0; i < 6; i++)
            {
                var t = from.AddMonths(i);
                var key = t.ToString("MMM yyyy", CultureInfo.InvariantCulture);
                var cQ = quots.Count(x => x.QuotationDate.Year == t.Year && x.QuotationDate.Month == t.Month);
                var cO = orders.Count(x => x.OrderDate.Year == t.Year && x.OrderDate.Month == t.Month);
                qm.Add(new LabelCountDto { Label = key, Count = cQ, Value = cQ });
                om.Add(new LabelCountDto { Label = key, Count = cO, Value = cO });
            }

            sales = new SalesModuleStats
            {
                QuotationDraft = qD,
                QuotationSent = qS,
                OrderOpen = oO,
                OrderConfirmed = oC,
                DeliveryChallanCount = dc,
                QuotationByMonth = qm,
                SalesOrderByMonth = om
            };
        }

        ProductionModuleStats? prod = null;
        if (showPr)
        {
            var bySt = await _db.ProductionOrders.AsNoTracking()
                .GroupBy(p => p.Status)
                .Select(g => new { g.Key, C = g.Count() })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var map = bySt.ToDictionary(x => x.Key, x => x.C);
            var fert = await _db.CreateMaterialMaster.AsNoTracking()
                .CountAsync(m => m.MaterialTypeCode == "FERT" || m.MaterialTypeCode == "HALB", ct)
                .ConfigureAwait(false);
            var ot = await _db.ProductionOrders.AsNoTracking()
                .CountAsync(p =>
                    p.ReleasedRoutingId != null
                    && (p.Status == ProductionOrder.StatusReleased || p.Status == ProductionOrder.StatusInProgress),
                    ct)
                .ConfigureAwait(false);
            var chart = bySt
                .OrderBy(x => x.Key)
                .Select(x => new LabelCountDto { Label = x.Key, Count = x.C, Value = x.C })
                .ToList();
            prod = new ProductionModuleStats
            {
                MrpBomCount = fert,
                ProductionOrdersByStatus = map,
                OperationTrackingOpen = ot,
                ProductionOrdersByStatusChart = chart
            };
        }

        return new DashboardPageVm
        {
            UserDisplayName = display,
            DepartmentNames = deptNames,
            TodayLabel = DateTime.Today.ToString("dddd, dd MMM yyyy", CultureInfo.InvariantCulture),
            ShowAdmin = showAd,
            ShowStore = showSt,
            ShowSales = showSa,
            ShowProduction = showPr,
            HasAnyModule = hasAny,
            Admin = admin,
            Store = store,
            Sales = sales,
            Production = prod
        };
    }

    public static string ChartJsonForLabelsValues(IReadOnlyList<string> labels, IReadOnlyList<int> values) =>
        JsonSerializer.Serialize(new { labels, datasets = new[] { new { label = "Count", data = values, backgroundColor = "rgba(0, 112, 242, 0.5)", borderColor = "rgb(0, 112, 242)", borderWidth = 1 } } });

    public static string ChartJsonDoughnut(IReadOnlyList<LabelCountDto> items, IReadOnlyList<string>? background = null) =>
        JsonSerializer.Serialize(new
        {
            labels = items.Select(i => i.Label).ToList(),
            datasets = new[] { new { data = items.Select(i => (double)i.Value).ToList(), backgroundColor = background ?? new[] { "#0070f2", "#27ae60", "#9b59b6", "#e67e22", "#e74c3c", "#95a5a6" } } }
        });

    public static string ChartJsonLineMonths(IReadOnlyList<LabelCountDto> q, string label, string color) =>
        JsonSerializer.Serialize(new
        {
            labels = q.Select(x => x.Label).ToList(),
            datasets = new[] { new { label, data = q.Select(x => (double)x.Count).ToList(), borderColor = color, backgroundColor = color + "33", tension = 0.25, fill = true } }
        });

    public static string ChartJsonLineQuotationsAndOrders(IReadOnlyList<LabelCountDto> quotations, IReadOnlyList<LabelCountDto> orders) =>
        JsonSerializer.Serialize(new
        {
            labels = quotations.Select(x => x.Label).ToList(),
            datasets = new[]
            {
                new
                {
                    label = "Quotations",
                    data = quotations.Select(x => (double)x.Count).ToList(),
                    borderColor = "rgb(0, 112, 242)",
                    backgroundColor = "rgba(0, 112, 242, 0.12)",
                    tension = 0.25,
                    fill = true
                },
                new
                {
                    label = "Sales orders",
                    data = orders.Select(x => (double)x.Count).ToList(),
                    borderColor = "rgb(39, 174, 96)",
                    backgroundColor = "rgba(39, 174, 96, 0.12)",
                    tension = 0.25,
                    fill = true
                }
            }
        });
}
