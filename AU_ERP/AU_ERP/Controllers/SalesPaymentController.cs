using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "SalesDepartment")]
public class SalesPaymentController : Controller
{
    private readonly AppDbContext _db;

    public SalesPaymentController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewData["Title"] = "Payments";
        List<SalesPayment> payments;
        try
        {
            payments = await _db.SalesPayments.AsNoTracking()
                .OrderByDescending(p => p.DocumentDate)
                .ThenByDescending(p => p.Id)
                .ToListAsync(ct);
        }
        catch (SqlException ex) when (
            ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) &&
            ex.Message.Contains("SalesPayments", StringComparison.OrdinalIgnoreCase))
        {
            TempData["SpError"] = "Sales payment table is missing in the current database. Run database migrations for this environment.";
            payments = new List<SalesPayment>();
        }

        var vm = new SalesPaymentIndexVm
        {
            Payments = payments.Select(p => new SalesPaymentListRowVm
            {
                Id = p.Id,
                DocumentNumber = p.DocumentNumber,
                DocumentDate = p.DocumentDate,
                InvoiceDocumentNumber = p.InvoiceDocumentNumber,
                DealerDisplayName = p.DealerDisplayName,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod
            }).ToList()
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct = default)
    {
        var p = await _db.SalesPayments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p == null)
            return NotFound();

        ViewData["Title"] = $"Payments — {p.DocumentNumber}";
        var vm = new SalesPaymentEditVm
        {
            Id = p.Id,
            DocumentNumber = p.DocumentNumber,
            DocumentDate = p.DocumentDate,
            InvoiceDocumentNumber = p.InvoiceDocumentNumber,
            DealerDisplayName = p.DealerDisplayName,
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            ChequeNumber = p.ChequeNumber
        };
        return View(vm);
    }

    public sealed class UpdateSalesPaymentForm
    {
        public int Id { get; set; }
        public DateTime DocumentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ChequeNumber { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [FromForm] UpdateSalesPaymentForm model, CancellationToken ct = default)
    {
        if (id != model.Id)
        {
            TempData["SpError"] = "Invalid request.";
            return RedirectToAction(nameof(Index));
        }

        if (!SalesPaymentMethodHelper.IsValidMethod(model.PaymentMethod))
        {
            TempData["SpError"] = "Invalid payment method.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var method = SalesPaymentMethodHelper.NormalizeMethod(model.PaymentMethod);
        var cheque = (model.ChequeNumber ?? "").Trim();
        if (method == SalesPayment.MethodCheque && string.IsNullOrEmpty(cheque))
        {
            TempData["SpError"] = "Cheque number is required when payment method is cheque.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var docDate = model.DocumentDate.Date;

        var p = await _db.SalesPayments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p == null)
            return NotFound();

        p.DocumentDate = docDate;
        p.PaymentMethod = method;
        p.ChequeNumber = method == SalesPayment.MethodCheque ? cheque[..Math.Min(64, cheque.Length)] : null;

        await _db.SaveChangesAsync(ct);
        TempData["SpMessage"] = "Payment updated.";
        return RedirectToAction(nameof(Index));
    }
}
