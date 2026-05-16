using System.Data;
using AU_ERP.Configuration;
using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using AU_ERP.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "SalesDepartment")]
public class ReturnOrderController : Controller
{
    private readonly AppDbContext _db;
    private readonly DocumentNumberAllocator _documentNumbers;
    private readonly SalesReturnCreditMemoPdfService _creditMemoPdf;
    private readonly CompanyInfoService _companyInfo;

    public ReturnOrderController(
        AppDbContext db,
        DocumentNumberAllocator documentNumbers,
        SalesReturnCreditMemoPdfService creditMemoPdf,
        CompanyInfoService companyInfo)
    {
        _db = db;
        _documentNumbers = documentNumbers;
        _creditMemoPdf = creditMemoPdf;
        _companyInfo = companyInfo;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken ct = default)
    {
        ViewData["Title"] = "Return order";
        var qq = (q ?? "").Trim();
        List<SalesReturnOrder> orders;
        try
        {
            var query = ApplyReturnOrderSearchFilter(_db.SalesReturnOrders.AsNoTracking(), qq);
            orders = await query
                .OrderByDescending(r => r.DocumentDate)
                .ThenByDescending(r => r.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
        catch (SqlException ex) when (
            ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) &&
            ex.Message.Contains("SalesReturnOrders", StringComparison.OrdinalIgnoreCase))
        {
            TempData["RoError"] = "Return order tables are missing. Run database migrations for this environment.";
            orders = new List<SalesReturnOrder>();
        }

        var cmDocByReturnOrderId = new Dictionary<int, string>();
        if (orders.Count > 0)
        {
            var ids = orders.Select(o => o.Id).ToList();
            try
            {
                var cmRows = await _db.SalesReturnCreditMemos.AsNoTracking()
                    .Where(c => ids.Contains(c.SalesReturnOrderId))
                    .Select(c => new { c.SalesReturnOrderId, c.DocumentNumber })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                foreach (var row in cmRows)
                    cmDocByReturnOrderId[row.SalesReturnOrderId] = row.DocumentNumber;
            }
            catch (SqlException ex) when (
                ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) &&
                ex.Message.Contains("SalesReturnCreditMemos", StringComparison.OrdinalIgnoreCase))
            {
                // Credit memo table missing; list still works without CM flags.
            }
        }

        var vm = new ReturnOrderIndexVm
        {
            FilterQuery = string.IsNullOrEmpty(qq) ? null : qq,
            Orders = orders.Select(r => new ReturnOrderIndexRowVm
            {
                Id = r.Id,
                DocumentNumber = r.DocumentNumber,
                DocumentDate = r.DocumentDate,
                InvoiceDocumentNumber = r.InvoiceDocumentNumber,
                DealerDisplayName = r.DealerDisplayName,
                ReturnReasonSnippet = Snippet(r.ReturnReason, 80),
                HasCreditMemo = cmDocByReturnOrderId.ContainsKey(r.Id),
                CreditMemoDocumentNumber = cmDocByReturnOrderId.TryGetValue(r.Id, out var dn) ? dn : null
            }).ToList()
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ReadOnlyPartial(int id, CancellationToken ct = default)
    {
        var r = await LoadReturnOrderForDetailAsync(id, ct).ConfigureAwait(false);
        if (r == null)
            return NotFound();
        return PartialView("_ReturnOrderReadOnlyModalBody", MapToDetailsVm(r));
    }

    [HttpGet]
    public async Task<IActionResult> CreditMemoPartial(int id, CancellationToken ct = default)
    {
        SalesReturnCreditMemo? cm;
        try
        {
            cm = await _db.SalesReturnCreditMemos.AsNoTracking()
                .Include(c => c.Lines).ThenInclude(l => l.QuantityUom)
                .FirstOrDefaultAsync(c => c.SalesReturnOrderId == id, ct)
                .ConfigureAwait(false);
        }
        catch (SqlException ex) when (
            ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) &&
            ex.Message.Contains("SalesReturnCreditMemos", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        if (cm == null)
            return NotFound();

        var vm = new ReturnOrderCreditMemoModalVm
        {
            ReturnOrderId = id,
            CreditMemo = cm
        };
        return PartialView("_ReturnOrderCreditMemoModalBody", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        var r = await LoadReturnOrderForDetailAsync(id, ct).ConfigureAwait(false);
        if (r == null)
            return NotFound();

        ViewData["Title"] = $"Return order {r.DocumentNumber}";
        var vm = MapToDetailsVm(r);
        return View(vm);
    }

    private static IQueryable<SalesReturnOrder> ApplyReturnOrderSearchFilter(IQueryable<SalesReturnOrder> query, string qq)
    {
        if (string.IsNullOrEmpty(qq))
            return query;
        return query.Where(r =>
            r.DocumentNumber.Contains(qq)
            || r.InvoiceDocumentNumber.Contains(qq)
            || (r.DealerDisplayName != null && r.DealerDisplayName.Contains(qq))
            || (r.DealerBusinessPartnerId != null && r.DealerBusinessPartnerId.Contains(qq))
            || r.ReturnReason.Contains(qq));
    }

    private async Task<SalesReturnOrder?> LoadReturnOrderForDetailAsync(int id, CancellationToken ct)
    {
        try
        {
            return await _db.SalesReturnOrders.AsNoTracking()
                .Include(x => x.Lines).ThenInclude(l => l.QuantityUom)
                .Include(x => x.SalesReturnCreditMemo)
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                .ConfigureAwait(false);
        }
        catch (SqlException ex) when (
            ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) &&
            ex.Message.Contains("SalesReturnCreditMemos", StringComparison.OrdinalIgnoreCase))
        {
            return await _db.SalesReturnOrders.AsNoTracking()
                .Include(x => x.Lines).ThenInclude(l => l.QuantityUom)
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                .ConfigureAwait(false);
        }
    }

    [HttpGet]
    public async Task<IActionResult> CreditMemo(int id, CancellationToken ct = default)
    {
        var cm = await _db.SalesReturnCreditMemos.AsNoTracking()
            .Include(c => c.Lines).ThenInclude(l => l.QuantityUom)
            .FirstOrDefaultAsync(c => c.SalesReturnOrderId == id, ct);
        if (cm == null)
            return NotFound();

        ViewData["Title"] = $"Credit memo {cm.DocumentNumber}";
        ViewBag.ReturnOrderId = id;
        var letterhead = await _companyInfo.GetPdfHeaderAsync(ct).ConfigureAwait(false);
        ViewBag.CompanyPrintName = letterhead.CompanyName;
        ViewBag.CompanyPrintPhone = letterhead.PhoneNumber;
        return View(cm);
    }

    [HttpGet]
    public async Task<IActionResult> CreditMemoPdf(int id, CancellationToken ct = default)
    {
        var cm = await _db.SalesReturnCreditMemos.AsNoTracking()
            .Include(c => c.Lines).ThenInclude(l => l.QuantityUom)
            .FirstOrDefaultAsync(c => c.SalesReturnOrderId == id, ct);
        if (cm == null)
            return NotFound();

        var bytes = await _creditMemoPdf.BuildPdfAsync(cm, ct).ConfigureAwait(false);
        var safeName = string.Join("_", (cm.DocumentNumber ?? "CreditMemo").Split(Path.GetInvalidFileNameChars()));
        return File(bytes, "application/pdf", $"{safeName}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> Create(int invoiceId, CancellationToken ct = default)
    {
        if (invoiceId <= 0)
        {
            TempData["RoError"] = "Invalid invoice.";
            return RedirectToAction(nameof(InvoiceController.Index), "Invoice");
        }

        var inv = await _db.SalesInvoices
            .AsNoTracking()
            .Include(i => i.Lines).ThenInclude(l => l.QuantityUom)
            .Include(i => i.DeliveryChallan)!.ThenInclude(dc => dc!.SalesOrder)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (inv == null)
            return NotFound();

        if (string.Equals(inv.Status, SalesInvoice.StatusReturned, StringComparison.OrdinalIgnoreCase))
        {
            TempData["RoError"] = "This invoice is already returned.";
            return RedirectToAction(nameof(InvoiceController.Index), "Invoice");
        }

        if (string.Equals(inv.Status, SalesInvoice.StatusReturnInProcess, StringComparison.OrdinalIgnoreCase))
        {
            TempData["RoError"] = "Return is in process; complete quality inspection from the invoice screen.";
            return RedirectToAction(nameof(InvoiceController.Index), "Invoice");
        }

        if (await _db.SalesReturnOrders.AsNoTracking().AnyAsync(r => r.SalesInvoiceId == invoiceId, ct))
        {
            TempData["RoError"] = "A return order already exists for this invoice.";
            return RedirectToAction(nameof(InvoiceController.Index), "Invoice");
        }

        if (inv.Lines == null || inv.Lines.Count == 0)
        {
            TempData["RoError"] = "Invoice has no lines to return.";
            return RedirectToAction(nameof(InvoiceController.Index), "Invoice");
        }

        var dc = inv.DeliveryChallan;
        var so = dc?.SalesOrder;
        var qtyTotal = inv.Lines.Sum(l => l.Quantity);

        var vm = new ReturnOrderCreateVm
        {
            InvoiceId = inv.Id,
            DocumentDate = DateTime.Today,
            DealerBusinessPartnerId = inv.DealerBusinessPartnerId,
            DealerDisplayName = inv.DealerDisplayName,
            SalesOrderNumber = so?.SalesOrderNumber ?? dc?.ReferenceSalesOrderNumber,
            DeliveryChallanDocumentDate = dc?.DocumentDate ?? inv.DocumentDate,
            SalesOrderRequestedDeliveryDate = so?.RequestedDeliveryDate,
            InvoiceDocumentNumber = inv.DocumentNumber,
            InvoiceGrandTotal = inv.GrandTotal,
            ItemsDeliveredQuantityTotal = qtyTotal,
            Lines = inv.Lines.OrderBy(l => l.LineNo).Select(l => new ReturnOrderCreateLineVm
            {
                SalesInvoiceLineId = l.Id,
                LineNo = l.LineNo,
                MaterialNumber = l.MaterialNumber,
                MaterialDescription = l.MaterialDescription,
                UomCode = l.QuantityUom?.Code,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                QuantityInvoiced = l.Quantity,
                QuantityReturned = 0m
            }).ToList()
        };

        ViewData["Title"] = "Create return order";
        return View(vm);
    }

    public sealed class ReturnOrderLineQtyFormRow
    {
        public int SalesInvoiceLineId { get; set; }
        public decimal QuantityReturned { get; set; }
    }

    public sealed class ReturnOrderCreateForm
    {
        public int InvoiceId { get; set; }
        public DateTime DocumentDate { get; set; }
        public string? ReturnReason { get; set; }
        public List<ReturnOrderLineQtyFormRow>? Lines { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] ReturnOrderCreateForm model, CancellationToken ct = default)
    {
        if (model.InvoiceId <= 0)
        {
            TempData["RoError"] = "Invalid invoice.";
            return RedirectToAction(nameof(InvoiceController.Index), "Invoice");
        }

        var reason = (model.ReturnReason ?? "").Trim();
        if (string.IsNullOrEmpty(reason))
        {
            TempData["RoError"] = "Return reason is required.";
            return RedirectToAction(nameof(Create), new { invoiceId = model.InvoiceId });
        }

        if (reason.Length > 2000)
            reason = reason[..2000];

        var inv = await _db.SalesInvoices
            .Include(i => i.Lines)
            .Include(i => i.DeliveryChallan)!.ThenInclude(dc => dc!.SalesOrder)
            .Include(i => i.DeliveryChallan)!.ThenInclude(dc => dc!.Items)
            .FirstOrDefaultAsync(i => i.Id == model.InvoiceId, ct);

        if (inv == null)
            return NotFound();

        if (string.Equals(inv.Status, SalesInvoice.StatusReturned, StringComparison.OrdinalIgnoreCase))
        {
            TempData["RoError"] = "This invoice is already returned.";
            return RedirectToAction(nameof(InvoiceController.Index), "Invoice");
        }

        if (string.Equals(inv.Status, SalesInvoice.StatusReturnInProcess, StringComparison.OrdinalIgnoreCase))
        {
            TempData["RoError"] = "Return is in process; complete quality inspection first.";
            return RedirectToAction(nameof(InvoiceController.Index), "Invoice");
        }

        if (await _db.SalesReturnOrders.AnyAsync(r => r.SalesInvoiceId == model.InvoiceId, ct))
        {
            TempData["RoError"] = "A return order already exists for this invoice.";
            return RedirectToAction(nameof(InvoiceController.Index), "Invoice");
        }

        if (inv.Lines == null || inv.Lines.Count == 0)
        {
            TempData["RoError"] = "Invoice has no lines.";
            return RedirectToAction(nameof(InvoiceController.Index), "Invoice");
        }

        var lineById = inv.Lines.ToDictionary(l => l.Id);
        var qtyByLineId = new Dictionary<int, decimal>();
        foreach (var row in model.Lines ?? new List<ReturnOrderLineQtyFormRow>())
        {
            if (row.SalesInvoiceLineId <= 0)
                continue;
            qtyByLineId[row.SalesInvoiceLineId] = row.QuantityReturned;
        }

        var anyPositive = false;
        foreach (var il in inv.Lines)
        {
            var q = qtyByLineId.TryGetValue(il.Id, out var v) ? v : 0m;
            if (DocumentQuantityRules.ValidateNonNegativeWhole(q, $"Return quantity (line {il.LineNo})") is { } rqErr)
            {
                TempData["RoError"] = rqErr;
                return RedirectToAction(nameof(Create), new { invoiceId = model.InvoiceId });
            }
            if (q < 0 || q > il.Quantity)
            {
                TempData["RoError"] = $"Return quantity must be between 0 and invoiced quantity for line {il.LineNo} ({il.MaterialNumber}).";
                return RedirectToAction(nameof(Create), new { invoiceId = model.InvoiceId });
            }

            if (q > 0)
                anyPositive = true;
        }

        if (!anyPositive)
        {
            TempData["RoError"] = "Enter a return quantity greater than zero on at least one line.";
            return RedirectToAction(nameof(Create), new { invoiceId = model.InvoiceId });
        }

        var docDate = model.DocumentDate.Date;
        var dc = inv.DeliveryChallan;
        var so = dc?.SalesOrder;
        var itemsQtyTotal = inv.Lines.Sum(l => l.Quantity);
        var plantId = (dc?.PlantId ?? "").Trim();
        if (plantId.Length == 0)
        {
            TempData["RoError"] = "Delivery challan must have a plant configured to process returns (quality inspection and inventory).";
            return RedirectToAction(nameof(Create), new { invoiceId = model.InvoiceId });
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                    .ConfigureAwait(false);
                try
                {
                    var invTracked = await _db.SalesInvoices
                        .Include(i => i.Lines)
                        .Include(i => i.DeliveryChallan)!.ThenInclude(d => d!.Items)
                        .FirstOrDefaultAsync(i => i.Id == model.InvoiceId, ct)
                        .ConfigureAwait(false);
                    if (invTracked == null)
                        throw new InvalidOperationException("Invoice not found.");
                    if (string.Equals(invTracked.Status, SalesInvoice.StatusReturned, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Invoice already returned.");
                    if (string.Equals(invTracked.Status, SalesInvoice.StatusReturnInProcess, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Return in process.");
                    if (await _db.SalesReturnOrders.AnyAsync(r => r.SalesInvoiceId == invTracked.Id, ct).ConfigureAwait(false))
                        throw new InvalidOperationException("Return order already exists.");

                    var dcTracked = invTracked.DeliveryChallan;
                    var plantTracked = (dcTracked?.PlantId ?? "").Trim();
                    if (plantTracked.Length == 0)
                        throw new InvalidOperationException("Delivery challan plant is required.");

                    var docNo = await _documentNumbers.AllocateAsync(ModuleKeys.ReturnOrder, ct).ConfigureAwait(false);

                    var header = new SalesReturnOrder
                    {
                        DocumentNumber = docNo,
                        DocumentDate = docDate,
                        SalesInvoiceId = invTracked.Id,
                        ReturnReason = reason,
                        DealerBusinessPartnerId = invTracked.DealerBusinessPartnerId,
                        DealerDisplayName = string.IsNullOrWhiteSpace(invTracked.DealerDisplayName)
                            ? null
                            : invTracked.DealerDisplayName![..Math.Min(500, invTracked.DealerDisplayName.Length)],
                        SalesOrderNumber = string.IsNullOrWhiteSpace(so?.SalesOrderNumber)
                            ? (dc?.ReferenceSalesOrderNumber != null
                                ? dc.ReferenceSalesOrderNumber[..Math.Min(40, dc.ReferenceSalesOrderNumber.Length)]
                                : null)
                            : so!.SalesOrderNumber[..Math.Min(40, so.SalesOrderNumber.Length)],
                        DeliveryChallanDocumentDate = dc?.DocumentDate ?? invTracked.DocumentDate,
                        SalesOrderRequestedDeliveryDate = so?.RequestedDeliveryDate,
                        InvoiceDocumentNumber = invTracked.DocumentNumber,
                        InvoiceGrandTotal = invTracked.GrandTotal,
                        ItemsDeliveredQuantityTotal = itemsQtyTotal,
                        CreatedAt = DateTime.UtcNow,
                        Lines = new List<SalesReturnOrderLine>()
                    };

                    foreach (var il in invTracked.Lines.OrderBy(l => l.LineNo))
                    {
                        var qRet = qtyByLineId.TryGetValue(il.Id, out var v) ? v : 0m;
                        if (qRet <= 0)
                            continue;
                        header.Lines.Add(new SalesReturnOrderLine
                        {
                            SalesInvoiceLineId = il.Id,
                            LineNo = il.LineNo,
                            MaterialNumber = il.MaterialNumber,
                            MaterialDescription = string.IsNullOrWhiteSpace(il.MaterialDescription)
                                ? null
                                : il.MaterialDescription![..Math.Min(500, il.MaterialDescription.Length)],
                            QuantityUomId = il.QuantityUomId,
                            UnitPrice = il.UnitPrice,
                            LineTotal = il.LineTotal,
                            QuantityInvoiced = il.Quantity,
                            QuantityReturned = qRet
                        });
                    }

                    // QI document number is the row Id (not document-integration ranges).
                    var qi = new SalesReturnQualityInspection
                    {
                        DocumentNumber = $"T{Guid.NewGuid():N}",
                        DocumentDate = DateTime.UtcNow.Date,
                        Status = SalesReturnQualityInspection.StatusPending,
                        PlantId = plantTracked,
                        CreatedAt = DateTime.UtcNow,
                        Lines = new List<SalesReturnQualityInspectionLine>()
                    };

                    foreach (var roLine in header.Lines)
                    {
                        var invLine = invTracked.Lines.First(l => l.Id == roLine.SalesInvoiceLineId);
                        var batch = ResolveDcBatch(dcTracked?.Items, invLine, roLine.MaterialNumber);
                        qi.Lines.Add(new SalesReturnQualityInspectionLine
                        {
                            SalesReturnOrderLine = roLine,
                            MaterialNumber = roLine.MaterialNumber,
                            MaterialDescription = roLine.MaterialDescription,
                            BatchNumber = string.IsNullOrWhiteSpace(batch) ? null : batch![..Math.Min(64, batch.Length)],
                            QuantityReturned = roLine.QuantityReturned,
                            QuantityUomId = roLine.QuantityUomId,
                            UnitPrice = roLine.UnitPrice,
                            ItemChargeValuesJson = string.IsNullOrWhiteSpace(invLine.ItemChargeValuesJson)
                                ? null
                                : invLine.ItemChargeValuesJson[..Math.Min(2000, invLine.ItemChargeValuesJson.Length)],
                            QtyBackToStock = roLine.QuantityReturned,
                            QtyConvertToRaw = 0m,
                            QtyScrap = 0m,
                            ConvertTargetMaterialNumber = null
                        });
                    }

                    header.SalesReturnQualityInspection = qi;

                    var cmDocNo = await _documentNumbers.AllocateAsync(ModuleKeys.CreditMemo, ct).ConfigureAwait(false);
                    var cm = new SalesReturnCreditMemo
                    {
                        DocumentNumber = cmDocNo,
                        DocumentDate = DateTime.UtcNow.Date,
                        ReturnOrderDocumentNumber = docNo,
                        SalesInvoiceId = invTracked.Id,
                        InvoiceDocumentNumber = invTracked.DocumentNumber,
                        DealerBusinessPartnerId = invTracked.DealerBusinessPartnerId,
                        DealerDisplayName = string.IsNullOrWhiteSpace(invTracked.DealerDisplayName)
                            ? null
                            : invTracked.DealerDisplayName![..Math.Min(500, invTracked.DealerDisplayName.Length)],
                        CreatedAt = DateTime.UtcNow,
                        Lines = new List<SalesReturnCreditMemoLine>()
                    };

                    foreach (var roLine in header.Lines)
                    {
                        var creditAmt = ComputeReturnedLineCredit(roLine.LineTotal, roLine.QuantityInvoiced, roLine.QuantityReturned);
                        cm.Lines.Add(new SalesReturnCreditMemoLine
                        {
                            SalesReturnOrderLine = roLine,
                            LineNo = roLine.LineNo,
                            MaterialNumber = roLine.MaterialNumber,
                            MaterialDescription = roLine.MaterialDescription,
                            QuantityReturned = roLine.QuantityReturned,
                            QuantityUomId = roLine.QuantityUomId,
                            UnitPrice = roLine.UnitPrice,
                            LineCreditAmount = creditAmt
                        });
                    }

                    cm.GrandTotalCredit = cm.Lines.Sum(l => l.LineCreditAmount);
                    header.SalesReturnCreditMemo = cm;

                    await _db.SalesReturnOrders.AddAsync(header, ct).ConfigureAwait(false);
                    invTracked.Status = SalesInvoice.StatusReturnInProcess;
                    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                    qi.DocumentNumber = qi.Id.ToString();
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
            TempData["RoError"] = ex.Message;
            return RedirectToAction(nameof(Create), new { invoiceId = model.InvoiceId });
        }
        catch (DocumentIntegrationRangeExhaustedException ex)
        {
            TempData["RoError"] = ex.Message;
            return RedirectToAction(nameof(Create), new { invoiceId = model.InvoiceId });
        }
        catch (Exception ex)
        {
            TempData["RoError"] = ex.InnerException?.Message ?? ex.Message;
            return RedirectToAction(nameof(Create), new { invoiceId = model.InvoiceId });
        }

        TempData["RoMessage"] = "Return order saved. Complete quality inspection (QI) from the invoice screen.";
        return RedirectToAction(nameof(Index));
    }

    private static string? ResolveDcBatch(
        ICollection<DeliveryChallanItem>? items,
        SalesInvoiceLine invLine,
        string returnMaterialNumber)
    {
        if (items == null || items.Count == 0)
            return null;
        var mat = (returnMaterialNumber ?? "").Trim();
        foreach (var it in items)
        {
            if (!string.Equals(it.MaterialNumber, mat, StringComparison.OrdinalIgnoreCase))
                continue;
            if (invLine.SourceSalesOrderItemId is int soi && soi > 0
                && it.SalesOrderItemId is int dciSoi && dciSoi == soi)
                return string.IsNullOrWhiteSpace(it.Batch) ? null : it.Batch.Trim();
        }

        foreach (var it in items)
        {
            if (string.Equals(it.MaterialNumber, mat, StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(it.Batch) ? null : it.Batch.Trim();
        }

        return null;
    }

    private static decimal ComputeReturnedLineCredit(decimal lineTotal, decimal quantityInvoiced, decimal quantityReturned)
    {
        if (quantityReturned <= 0 || quantityInvoiced <= 0)
            return 0m;
        var raw = lineTotal * (quantityReturned / quantityInvoiced);
        return Math.Round(Math.Min(lineTotal, raw), 4, MidpointRounding.AwayFromZero);
    }

    private static ReturnOrderDetailsVm MapToDetailsVm(SalesReturnOrder r)
    {
        var cm = r.SalesReturnCreditMemo;
        return new ReturnOrderDetailsVm
        {
            Id = r.Id,
            DocumentNumber = r.DocumentNumber,
            DocumentDate = r.DocumentDate,
            ReturnReason = r.ReturnReason,
            InvoiceDocumentNumber = r.InvoiceDocumentNumber,
            SalesInvoiceId = r.SalesInvoiceId,
            DealerBusinessPartnerId = r.DealerBusinessPartnerId,
            DealerDisplayName = r.DealerDisplayName,
            SalesOrderNumber = r.SalesOrderNumber,
            DeliveryChallanDocumentDate = r.DeliveryChallanDocumentDate,
            SalesOrderRequestedDeliveryDate = r.SalesOrderRequestedDeliveryDate,
            InvoiceGrandTotal = r.InvoiceGrandTotal,
            ItemsDeliveredQuantityTotal = r.ItemsDeliveredQuantityTotal,
            CreditMemoId = cm?.Id,
            CreditMemoDocumentNumber = cm?.DocumentNumber,
            CreditMemoGrandTotal = cm?.GrandTotalCredit,
            Lines = r.Lines.OrderBy(l => l.LineNo).Select(l => new ReturnOrderCreateLineVm
            {
                SalesInvoiceLineId = l.SalesInvoiceLineId,
                LineNo = l.LineNo,
                MaterialNumber = l.MaterialNumber,
                MaterialDescription = l.MaterialDescription,
                UomCode = l.QuantityUom?.Code,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                QuantityInvoiced = l.QuantityInvoiced,
                QuantityReturned = l.QuantityReturned
            }).ToList()
        };
    }

    private static string Snippet(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";
        var t = text.Trim();
        return t.Length <= max ? t : t[..max] + "…";
    }
}
