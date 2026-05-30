using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

/// <summary>Lists sales return credit memos (financial documents linked to return orders).</summary>
[Authorize(Policy = "SalesDepartment")]
public class CreditMemoController : Controller
{
    private readonly AppDbContext _db;

    public CreditMemoController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        string? plantId,
        DateTime? docFrom,
        DateTime? docTo,
        int? openRo,
        CancellationToken ct = default)
    {
        ViewData["Title"] = "Credit memo";
        var search = (q ?? "").Trim();

        DateTime? dFrom = docFrom?.Date;
        DateTime? dTo = docTo?.Date;
        if (dFrom.HasValue && dTo.HasValue && dFrom.Value > dTo.Value)
            (dFrom, dTo) = (dTo, dFrom);

        int? validatedOpenRo = null;
        if (openRo is > 0)
        {
            try
            {
                var cmExists = await _db.SalesReturnCreditMemos.AsNoTracking()
                    .AnyAsync(c => c.SalesReturnOrderId == openRo.Value, ct)
                    .ConfigureAwait(false);
                if (cmExists)
                    validatedOpenRo = openRo.Value;
            }
            catch (SqlException)
            {
                /* ignore → no auto modal */
            }
        }

        var allPlants = await _db.PlantsSamples.AsNoTracking().OrderBy(p => p.PlantName).ToListAsync(ct).ConfigureAwait(false);
        var plantScope = await SalesPlantAccess.ResolveAsync(_db, User, plantId, allPlants.Select(p => p.PlantID), ct).ConfigureAwait(false);
        SalesPlantAccess.SetViewBag(this, plantScope, allPlants);

        List<CreditMemoIndexRowVm> rows;
        try
        {
            var joined = from cm in _db.SalesReturnCreditMemos.AsNoTracking()
                join ro in _db.SalesReturnOrders.AsNoTracking() on cm.SalesReturnOrderId equals ro.Id
                join inv in _db.SalesInvoices.AsNoTracking() on ro.SalesInvoiceId equals inv.Id
                join dc in _db.DeliveryChallans.AsNoTracking() on inv.DeliveryChallanId equals dc.Id
                select new { cm, PlantId = dc.PlantId };
            joined = SalesPlantAccess.ApplyListingPlantFilter(joined, plantScope, x => x.PlantId);
            var query = ApplyCreditMemoFilters(joined.Select(x => x.cm), search, dFrom, dTo);

            rows = await query
                .OrderByDescending(c => c.DocumentDate)
                .ThenByDescending(c => c.Id)
                .Select(c => new CreditMemoIndexRowVm
                {
                    Id = c.Id,
                    SalesReturnOrderId = c.SalesReturnOrderId,
                    DocumentNumber = c.DocumentNumber,
                    DocumentDate = c.DocumentDate,
                    ReturnOrderDocumentNumber = c.ReturnOrderDocumentNumber,
                    InvoiceDocumentNumber = c.InvoiceDocumentNumber,
                    DealerDisplayName = c.DealerDisplayName,
                    GrandTotalCredit = c.GrandTotalCredit
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
        catch (SqlException ex) when (
            ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) &&
            ex.Message.Contains("SalesReturnCreditMemos", StringComparison.OrdinalIgnoreCase))
        {
            TempData["CmError"] = "Credit memo tables are missing. Run database migrations.";
            rows = new List<CreditMemoIndexRowVm>();
        }

        var vm = new CreditMemoIndexVm
        {
            SearchQuery = string.IsNullOrEmpty(search) ? null : search,
            DocumentDateFrom = dFrom,
            DocumentDateTo = dTo,
            OpenReturnOrderId = validatedOpenRo,
            Items = rows
        };
        ViewBag.FilterPlantId = plantScope.EffectiveListPlantId;

        return View(vm);
    }

    private static IQueryable<SalesReturnCreditMemo> ApplyCreditMemoFilters(
        IQueryable<SalesReturnCreditMemo> q,
        string search,
        DateTime? docFrom,
        DateTime? docTo)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c =>
                c.DocumentNumber.Contains(s)
                || c.ReturnOrderDocumentNumber.Contains(s)
                || c.InvoiceDocumentNumber.Contains(s)
                || (c.DealerDisplayName != null && c.DealerDisplayName.Contains(s))
                || (c.DealerBusinessPartnerId != null && c.DealerBusinessPartnerId.Contains(s)));
        }

        if (docFrom.HasValue)
            q = q.Where(c => c.DocumentDate >= docFrom.Value);
        if (docTo.HasValue)
            q = q.Where(c => c.DocumentDate <= docTo.Value);

        return q;
    }
}
