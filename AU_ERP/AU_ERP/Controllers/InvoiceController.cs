using System.Data;
using AU_ERP.Configuration;
using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "SalesDepartment")]
public class InvoiceController : Controller
{
    private readonly AppDbContext _db;
    private readonly DocumentNumberAllocator _documentNumbers;

    public InvoiceController(AppDbContext db, DocumentNumberAllocator documentNumbers)
    {
        _db = db;
        _documentNumbers = documentNumbers;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        string? status,
        DateTime? docFrom,
        DateTime? docTo,
        CancellationToken ct = default)
    {
        ViewData["Title"] = "Invoice";
        var today = DateTime.Today;
        var search = (q ?? "").Trim();

        DateTime? dFrom = docFrom?.Date;
        DateTime? dTo = docTo?.Date;
        if (dFrom.HasValue && dTo.HasValue && dFrom.Value > dTo.Value)
            (dFrom, dTo) = (dTo, dFrom);

        var baseQ = _db.SalesInvoices.AsNoTracking();
        baseQ = ApplyInvoiceIndexFilters(baseQ, search, status, dFrom, dTo, today);

        var openQ = baseQ.Where(i => i.Status == SalesInvoice.StatusOpen && i.DueDate >= today);
        var overdueQ = baseQ.Where(i => i.Status == SalesInvoice.StatusOpen && i.DueDate < today);
        var collectedQ = baseQ.Where(i => i.Status == SalesInvoice.StatusCollected);
        var ripQ = baseQ.Where(i => i.Status == SalesInvoice.StatusReturnInProcess);

        var vm = new InvoiceIndexVm
        {
            SearchQuery = string.IsNullOrEmpty(search) ? null : search,
            StatusFilter = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            DocumentDateFrom = dFrom,
            DocumentDateTo = dTo,
            OpenCount = await openQ.CountAsync(ct),
            OpenAmount = await SumGrandTotalAsync(openQ, ct),
            OverdueCount = await overdueQ.CountAsync(ct),
            OverdueAmount = await SumGrandTotalAsync(overdueQ, ct),
            CollectedCount = await collectedQ.CountAsync(ct),
            CollectedAmount = await SumGrandTotalAsync(collectedQ, ct),
            ReturnInProcessCount = await ripQ.CountAsync(ct),
            ReturnInProcessAmount = await SumGrandTotalAsync(ripQ, ct),
            Invoices = await ApplyInvoiceIndexFilters(_db.SalesInvoices.AsNoTracking(), search, status, dFrom, dTo, today)
                .Include(i => i.DeliveryChallan)
                .OrderByDescending(i => i.DocumentDate)
                .ThenByDescending(i => i.Id)
                .ToListAsync(ct)
        };

        var invIds = vm.Invoices.Select(i => i.Id).ToList();
        if (invIds.Count > 0)
        {
            try
            {
                var roRows = await _db.SalesReturnOrders.AsNoTracking()
                    .Where(r => invIds.Contains(r.SalesInvoiceId))
                    .OrderByDescending(r => r.DocumentDate).ThenByDescending(r => r.Id)
                    .Select(r => new
                    {
                        r.SalesInvoiceId,
                        r.Id,
                        r.DocumentNumber,
                        r.DocumentDate
                    })
                    .ToListAsync(ct);

                vm.ReturnOrdersByInvoiceId = roRows
                    .GroupBy(x => x.SalesInvoiceId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => new InvoiceReturnOrderSummaryVm
                            {
                                Id = x.Id,
                                DocumentNumber = x.DocumentNumber,
                                DocumentDate = x.DocumentDate
                            })
                            .ToList());
            }
            catch (SqlException)
            {
                vm.ReturnOrdersByInvoiceId = new Dictionary<int, List<InvoiceReturnOrderSummaryVm>>();
            }

            try
            {
                var qiRows = await (
                    from ro in _db.SalesReturnOrders.AsNoTracking()
                    join qi in _db.SalesReturnQualityInspections.AsNoTracking() on ro.Id equals qi.SalesReturnOrderId
                    where invIds.Contains(ro.SalesInvoiceId)
                    select new { ro.SalesInvoiceId, qi.Id, qi.Status }).ToListAsync(ct);
                vm.QiByInvoiceId = qiRows
                    .GroupBy(x => x.SalesInvoiceId)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            var x = g.OrderByDescending(x => x.Id).First();
                            return new InvoiceQiNavInfo
                            {
                                QiId = x.Id,
                                IsPending = string.Equals(x.Status, SalesReturnQualityInspection.StatusPending,
                                    StringComparison.OrdinalIgnoreCase)
                            };
                        });
            }
            catch (SqlException)
            {
                vm.QiByInvoiceId = new Dictionary<int, InvoiceQiNavInfo>();
            }

            try
            {
                var cmRows = await (
                    from cm in _db.SalesReturnCreditMemos.AsNoTracking()
                    join ro in _db.SalesReturnOrders.AsNoTracking() on cm.SalesReturnOrderId equals ro.Id
                    where invIds.Contains(ro.SalesInvoiceId)
                    select new { ro.SalesInvoiceId, ro.Id }).ToListAsync(ct).ConfigureAwait(false);
                vm.CreditMemoReturnOrderIdByInvoiceId = cmRows
                    .GroupBy(x => x.SalesInvoiceId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First().Id);
            }
            catch (SqlException)
            {
                vm.CreditMemoReturnOrderIdByInvoiceId = new Dictionary<int, int>();
            }
        }

        return View(vm);
    }

    /// <summary>HTML table of return orders for an invoice — used inside the Invoice screen modal.</summary>
    [HttpGet]
    public async Task<IActionResult> ReturnOrdersModalList(int invoiceId, CancellationToken ct = default)
    {
        if (invoiceId <= 0)
            return BadRequest();

        string? invoiceDocNum;
        try
        {
            invoiceDocNum = await _db.SalesInvoices.AsNoTracking()
                .Where(i => i.Id == invoiceId)
                .Select(i => i.DocumentNumber)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }
        catch (SqlException)
        {
            return NotFound();
        }

        if (invoiceDocNum == null)
            return NotFound();

        List<InvoiceReturnOrderSummaryVm> orders;
        try
        {
            orders = await _db.SalesReturnOrders.AsNoTracking()
                .Where(r => r.SalesInvoiceId == invoiceId)
                .OrderByDescending(r => r.DocumentDate).ThenByDescending(r => r.Id)
                .Select(r => new InvoiceReturnOrderSummaryVm
                {
                    Id = r.Id,
                    DocumentNumber = r.DocumentNumber,
                    DocumentDate = r.DocumentDate
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
        catch (SqlException)
        {
            orders = new List<InvoiceReturnOrderSummaryVm>();
        }

        var vm = new InvoiceReturnOrdersListVm
        {
            InvoiceId = invoiceId,
            InvoiceDocumentNumber = invoiceDocNum,
            Orders = orders
        };
        return PartialView("_InvoiceReturnOrdersListPartial", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        var inv = await _db.SalesInvoices.AsNoTracking()
            .Include(i => i.Lines).ThenInclude(l => l.QuantityUom)
            .Include(i => i.DeliveryChallan)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inv == null)
            return NotFound();
        ViewData["Title"] = $"Invoice {inv.DocumentNumber}";
        ViewBag.CanPay = inv.Status == SalesInvoice.StatusOpen
            && !await _db.SalesPayments.AsNoTracking().AnyAsync(p => p.SalesInvoiceId == id, ct);
        int? returnOrderId = null;
        try
        {
            var ret = await _db.SalesReturnOrders.AsNoTracking()
                .FirstOrDefaultAsync(r => r.SalesInvoiceId == id, ct);
            returnOrderId = ret?.Id;
        }
        catch (SqlException)
        {
            returnOrderId = null;
        }

        int? qiId = null;
        var qiPending = false;
        try
        {
            var qrow = await (
                from ro in _db.SalesReturnOrders.AsNoTracking()
                join qi in _db.SalesReturnQualityInspections.AsNoTracking() on ro.Id equals qi.SalesReturnOrderId
                where ro.SalesInvoiceId == id
                select new { qi.Id, qi.Status }).FirstOrDefaultAsync(ct);
            if (qrow != null)
            {
                qiId = qrow.Id;
                qiPending = string.Equals(qrow.Status, SalesReturnQualityInspection.StatusPending, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (SqlException)
        {
            qiId = null;
            qiPending = false;
        }

        ViewBag.HasReturnOrder = returnOrderId != null;
        ViewBag.ReturnOrderId = returnOrderId;
        ViewBag.QiId = qiId;
        ViewBag.QiIsPending = qiPending;
        ViewBag.CanCreateReturn = !string.Equals(inv.Status, SalesInvoice.StatusReturned, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(inv.Status, SalesInvoice.StatusReturnInProcess, StringComparison.OrdinalIgnoreCase)
            && returnOrderId == null;
        ViewBag.CanOpenQi = qiId != null && qiPending;
        ViewBag.CanViewQi = qiId != null && !qiPending;
        int? creditMemoReturnOrderId = null;
        if (returnOrderId.HasValue)
        {
            try
            {
                var hasCm = await _db.SalesReturnCreditMemos.AsNoTracking()
                    .AnyAsync(c => c.SalesReturnOrderId == returnOrderId.Value, ct)
                    .ConfigureAwait(false);
                if (hasCm)
                    creditMemoReturnOrderId = returnOrderId;
            }
            catch (SqlException)
            {
                creditMemoReturnOrderId = null;
            }
        }

        ViewBag.CreditMemoReturnOrderId = creditMemoReturnOrderId;
        return View(inv);
    }

    [HttpGet]
    public async Task<JsonResult> PaymentPrefill(int invoiceId, CancellationToken ct = default)
    {
        var inv = await _db.SalesInvoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (inv == null)
            return Json(new { success = false, message = "Invoice not found." });
        if (inv.Status != SalesInvoice.StatusOpen)
            return Json(new { success = false, message = "Payment can only be recorded for open invoices." });

        var hasPayment = await _db.SalesPayments.AnyAsync(p => p.SalesInvoiceId == invoiceId, ct);
        if (hasPayment)
            return Json(new { success = false, message = "A payment already exists for this invoice." });

        string suggested = SalesPayment.MethodCash;
        if (!string.IsNullOrWhiteSpace(inv.DealerBusinessPartnerId))
        {
            var bpMethods = await _db.BusinessPartnerMasterSamples.AsNoTracking()
                .Where(b => b.BPID == inv.DealerBusinessPartnerId)
                .Select(b => b.PaymentMethods)
                .FirstOrDefaultAsync(ct);
            suggested = SalesPaymentMethodHelper.SuggestFromBusinessPartner(bpMethods);
        }

        var dealer = inv.DealerDisplayName ?? inv.DealerBusinessPartnerId ?? "";
        var today = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        return Json(new
        {
            success = true,
            documentDate = today,
            invoiceNumber = inv.DocumentNumber,
            dealerId = inv.DealerBusinessPartnerId,
            dealerName = dealer,
            amount = inv.GrandTotal,
            suggestedPaymentMethod = suggested
        });
    }

    public sealed class CreatePaymentForm
    {
        public int InvoiceId { get; set; }
        public DateTime DocumentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ChequeNumber { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayment([FromForm] CreatePaymentForm model, CancellationToken ct = default)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (model.InvoiceId <= 0)
        {
            if (isAjax) return Json(new { success = false, message = "Invalid invoice." });
            TempData["InvoiceError"] = "Invalid invoice.";
            return RedirectToAction(nameof(Index));
        }

        if (!SalesPaymentMethodHelper.IsValidMethod(model.PaymentMethod))
        {
            if (isAjax) return Json(new { success = false, message = "Invalid payment method." });
            TempData["InvoiceError"] = "Invalid payment method.";
            return RedirectToAction(nameof(Index));
        }

        var method = SalesPaymentMethodHelper.NormalizeMethod(model.PaymentMethod);
        var cheque = (model.ChequeNumber ?? "").Trim();
        if (method == SalesPayment.MethodCheque && string.IsNullOrEmpty(cheque))
        {
            if (isAjax) return Json(new { success = false, message = "Cheque number is required when payment method is cheque." });
            TempData["InvoiceError"] = "Cheque number is required when payment method is cheque.";
            return RedirectToAction(nameof(Index));
        }

        var docDate = model.DocumentDate.Date;

        var strategy = _db.Database.CreateExecutionStrategy();
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                    .ConfigureAwait(false);
                try
                {
                    var inv = await _db.SalesInvoices
                        .FirstOrDefaultAsync(i => i.Id == model.InvoiceId, ct)
                        .ConfigureAwait(false);
                    if (inv == null)
                        throw new InvalidOperationException("Invoice not found.");
                    if (inv.Status != SalesInvoice.StatusOpen)
                        throw new InvalidOperationException("Invoice is not open for payment.");
                    if (await _db.SalesPayments.AnyAsync(p => p.SalesInvoiceId == inv.Id, ct).ConfigureAwait(false))
                        throw new InvalidOperationException("A payment already exists for this invoice.");

                    var payNum = await _documentNumbers.AllocateAsync(ModuleKeys.Payment, ct).ConfigureAwait(false);

                    var payment = new SalesPayment
                    {
                        DocumentNumber = payNum,
                        DocumentDate = docDate,
                        SalesInvoiceId = inv.Id,
                        InvoiceDocumentNumber = inv.DocumentNumber,
                        DealerBusinessPartnerId = inv.DealerBusinessPartnerId,
                        DealerDisplayName = string.IsNullOrWhiteSpace(inv.DealerDisplayName)
                            ? null
                            : inv.DealerDisplayName![..Math.Min(500, inv.DealerDisplayName.Length)],
                        Amount = inv.GrandTotal,
                        PaymentMethod = method,
                        ChequeNumber = method == SalesPayment.MethodCheque ? cheque[..Math.Min(64, cheque.Length)] : null,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _db.SalesPayments.AddAsync(payment, ct).ConfigureAwait(false);
                    inv.Status = SalesInvoice.StatusCollected;
                    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                    await tx.CommitAsync(ct).ConfigureAwait(false);
                }
                catch
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    throw;
                }
            }).ConfigureAwait(false);
        }
        catch (DocumentIntegrationMissingException ex)
        {
            if (isAjax) return Json(new { success = false, message = ex.Message });
            TempData["InvoiceError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (DocumentIntegrationRangeExhaustedException ex)
        {
            if (isAjax) return Json(new { success = false, message = ex.Message });
            TempData["InvoiceError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["InvoiceError"] = msg;
            return RedirectToAction(nameof(Index));
        }

        if (isAjax) return Json(new { success = true, message = "Payment recorded." });
        TempData["InvoiceMessage"] = "Payment recorded.";
        return RedirectToAction(nameof(Index));
    }

    private static async Task<decimal> SumGrandTotalAsync(IQueryable<SalesInvoice> q, CancellationToken ct) =>
        await q.Select(i => i.GrandTotal).DefaultIfEmpty().SumAsync(ct);

    internal const string InvoiceIndexStatusFilterOpen = SalesInvoice.StatusOpen;
    internal const string InvoiceIndexStatusFilterOverdue = "Overdue";
    internal const string InvoiceIndexStatusFilterCollected = SalesInvoice.StatusCollected;
    internal const string InvoiceIndexStatusFilterReturnInProcess = SalesInvoice.StatusReturnInProcess;
    internal const string InvoiceIndexStatusFilterReturned = SalesInvoice.StatusReturned;

    private static IQueryable<SalesInvoice> ApplyInvoiceIndexFilters(
        IQueryable<SalesInvoice> q,
        string search,
        string? status,
        DateTime? docFrom,
        DateTime? docTo,
        DateTime today)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(i =>
                i.DocumentNumber.Contains(s)
                || i.DcNumber.Contains(s)
                || (i.DealerDisplayName != null && i.DealerDisplayName.Contains(s))
                || (i.DealerBusinessPartnerId != null && i.DealerBusinessPartnerId.Contains(s)));
        }

        if (docFrom.HasValue)
            q = q.Where(i => i.DocumentDate >= docFrom.Value);
        if (docTo.HasValue)
            q = q.Where(i => i.DocumentDate <= docTo.Value);

        var sf = (status ?? "").Trim();
        if (string.IsNullOrEmpty(sf))
            return q;

        if (string.Equals(sf, InvoiceIndexStatusFilterOverdue, StringComparison.OrdinalIgnoreCase))
            return q.Where(i => i.Status == SalesInvoice.StatusOpen && i.DueDate < today);

        // "Open" = open invoices that are not yet overdue relative to DueDate (same KPI as Open bucket).
        if (string.Equals(sf, InvoiceIndexStatusFilterOpen, StringComparison.OrdinalIgnoreCase))
            return q.Where(i => i.Status == SalesInvoice.StatusOpen && i.DueDate >= today);

        if (string.Equals(sf, InvoiceIndexStatusFilterCollected, StringComparison.OrdinalIgnoreCase))
            return q.Where(i => i.Status == SalesInvoice.StatusCollected);

        if (string.Equals(sf, InvoiceIndexStatusFilterReturnInProcess, StringComparison.OrdinalIgnoreCase))
            return q.Where(i => i.Status == SalesInvoice.StatusReturnInProcess);

        if (string.Equals(sf, InvoiceIndexStatusFilterReturned, StringComparison.OrdinalIgnoreCase))
            return q.Where(i => i.Status == SalesInvoice.StatusReturned);

        return q.Where(i => i.Status == sf);
    }
}
