using System.Data;
using System.Globalization;
using System.IO;
using AU_ERP.Configuration;
using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "SalesDepartment")]
public class DeliveryChallanController : Controller
{
    private readonly AppDbContext _db;
    private readonly DocumentNumberAllocator _documentNumbers;
    private readonly SalesInvoiceFromDeliveryChallanService _invoiceFromDc;

    public DeliveryChallanController(
        AppDbContext db,
        DocumentNumberAllocator documentNumbers,
        SalesInvoiceFromDeliveryChallanService invoiceFromDc)
    {
        _db = db;
        _documentNumbers = documentNumbers;
        _invoiceFromDc = invoiceFromDc;
    }

    [HttpGet]
    public async Task<JsonResult> MaterialUomContextForDeliveryChallan(string? materialNumber, int? includeUomIdForEdit, CancellationToken ct = default)
    {
        var (success, errorMessage, data) = await MaterialUomForMaterialHelper
            .TryBuildMaterialUomContextAsync(_db, materialNumber, includeUomIdForEdit, ct)
            .ConfigureAwait(false);
        if (!success)
            return Json(new { success = false, message = errorMessage });
        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        string? plantId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? fromSalesOrderId,
        CancellationToken ct = default)
    {
        ViewBag.FromSalesOrderId = fromSalesOrderId;
        var vm = new DeliveryChallanListVm
        {
            Q = q,
            PlantId = plantId,
            DateFrom = dateFrom,
            DateTo = dateTo
        };
        IQueryable<DeliveryChallan> query = _db.DeliveryChallans.AsNoTracking()
            .Include(d => d.Plant)
            .Include(d => d.ShipToBusinessPartner)
            .Include(d => d.SalesOrder)
            .Include(d => d.Items);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim();
            query = query.Where(d =>
                d.DeliveryChallanNumber.Contains(t) ||
                (d.ReferenceSalesOrderNumber != null && d.ReferenceSalesOrderNumber.Contains(t)) ||
                (d.ShipToDisplayName != null && d.ShipToDisplayName.Contains(t)) ||
                (d.SalesOrder != null && d.SalesOrder.SalesOrderNumber.Contains(t)));
        }
        if (!string.IsNullOrWhiteSpace(plantId))
            query = query.Where(d => d.PlantId == plantId);
        if (dateFrom is { } df)
        {
            var d0 = df.Date;
            query = query.Where(d => d.DocumentDate >= d0);
        }
        if (dateTo is { } dt2)
        {
            var d1 = dt2.Date;
            query = query.Where(d => d.DocumentDate <= d1);
        }

        vm.Items = await query
            .OrderByDescending(d => d.DocumentDate)
            .ThenByDescending(d => d.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var plants = await _db.PlantsSamples.AsNoTracking().OrderBy(p => p.PlantName).ToListAsync(ct).ConfigureAwait(false);
        var customers = await _db.BusinessPartnerMasterSamples.AsNoTracking()
            .OrderBy(c => c.FullName)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        ViewBag.Plants = plants;
        ViewBag.Customers = customers;
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        var d = await _db.DeliveryChallans.AsNoTracking()
            .Include(x => x.Plant)
            .Include(x => x.ShipToBusinessPartner)
            .Include(x => x.SalesOrder)
            .Include(x => x.Items)!.ThenInclude(i => i.QuantityUom)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);
        if (d == null)
            return NotFound();
        return View(d);
    }

    /// <summary>Read-only details HTML for the list “View” modal (no layout).</summary>
    [HttpGet]
    public async Task<IActionResult> DetailsModal(int id, CancellationToken ct = default)
    {
        var d = await _db.DeliveryChallans.AsNoTracking()
            .Include(x => x.Plant)
            .Include(x => x.ShipToBusinessPartner)
            .Include(x => x.SalesOrder)
            .Include(x => x.Items)!.ThenInclude(i => i.QuantityUom)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);
        if (d == null)
            return NotFound();
        return PartialView("_DetailsModal", d);
    }

    /// <summary>Confirmed sales orders that do not already have a delivery challan (searchable list).</summary>
    [HttpGet]
    public async Task<JsonResult> SearchSalesOrdersForChallan(string? q, CancellationToken ct = default)
    {
        var withDc = await _db.DeliveryChallans.AsNoTracking()
            .Where(d => d.SalesOrderId != null)
            .Select(d => d.SalesOrderId!.Value)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var withDcSet = withDc.ToHashSet();
        IQueryable<SalesOrder> query = _db.SalesOrders.AsNoTracking()
            .Where(x => x.Status == SalesOrder.StatusConfirmed);
        var receivedGiSoIds = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .Where(g => g.Status == SalesGoodsIssueDocument.StatusReceived)
            .Select(g => g.SalesOrderId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var receivedGiSet = receivedGiSoIds.ToHashSet();
        query = query.Where(x => receivedGiSet.Contains(x.Id));
        if (withDcSet.Count > 0)
            query = query.Where(x => !withDcSet.Contains(x.Id));
        var t = (q ?? "").Trim();
        if (t.Length > 0)
        {
            query = query.Where(x =>
                x.SalesOrderNumber.Contains(t)
                || (x.CustomerName != null && x.CustomerName.Contains(t)));
        }
        var items = await query
            .OrderByDescending(x => x.OrderDate)
            .ThenBy(x => x.SalesOrderNumber)
            .Take(40)
            .Select(x => new
            {
                id = x.Id,
                salesOrderNumber = x.SalesOrderNumber,
                customerName = x.CustomerName ?? "",
                orderDate = x.OrderDate,
                plantId = x.PlantId,
                shipToBusinessPartnerId = x.CustomerBusinessPartnerId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return Json(new { success = true, items });
    }

    [HttpGet]
    public async Task<JsonResult> GetSalesOrderLinesForChallan(int? salesOrderId, CancellationToken ct = default)
    {
        if (salesOrderId is not > 0)
            return Json(new { success = false, message = "Invalid order." });
        var hasDc = await _db.DeliveryChallans.AsNoTracking()
            .AnyAsync(d => d.SalesOrderId == salesOrderId, ct)
            .ConfigureAwait(false);
        if (hasDc)
            return Json(new { success = false, message = "This order already has a delivery challan." });
        var o = await _db.SalesOrders.AsNoTracking()
            .Include(x => x.Items)!.ThenInclude(i => i.QuantityUom)
            .FirstOrDefaultAsync(x => x.Id == salesOrderId, ct)
            .ConfigureAwait(false);
        if (o == null)
            return Json(new { success = false, message = "Sales order not found." });
        if (o.Status != SalesOrder.StatusConfirmed)
            return Json(new { success = false, message = "Only confirmed sales orders are listed." });
        var giReceived = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .AnyAsync(g => g.SalesOrderId == o.Id && g.Status == SalesGoodsIssueDocument.StatusReceived, ct)
            .ConfigureAwait(false);
        if (!giReceived)
            return Json(new { success = false, message = "Sales goods issue must be received before delivery challan." });
        var lines = o.Items
            .OrderBy(i => i.Id)
            .Select(i => new
            {
                salesOrderItemId = i.Id,
                materialNumber = i.MaterialNumber,
                materialDescription = i.MaterialDescription ?? "",
                orderQuantity = i.OrderQuantity,
                quantityUomId = i.QuantityUomId,
                uomCode = i.QuantityUom != null ? i.QuantityUom.Code : ""
            })
            .ToList();
        return Json(new
        {
            success = true,
            salesOrderId = o.Id,
            salesOrderNumber = o.SalesOrderNumber,
            plantId = o.PlantId,
            shipToBusinessPartnerId = o.CustomerBusinessPartnerId,
            lines
        });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken ct = default)
    {
        var d = await LoadDeliveryChallanForPdfAsync(id, ct).ConfigureAwait(false);
        if (d == null)
            return NotFound();
        var bytes = DeliveryChallanPdfService.BuildPdf(d, d.Items.ToList());
        var fileName = SafePdfName(d.DeliveryChallanNumber, "challan");
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<JsonResult> GetMaterialDescription(string? materialNumber, CancellationToken ct = default)
    {
        var m = (materialNumber ?? "").Trim();
        if (m.Length == 0)
            return Json(new { success = false });
        var d = await _db.CreateMaterialMaster.AsNoTracking()
            .Where(x => x.MaterialNumber == m)
            .Select(x => x.Description)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return Json(new { success = true, description = d ?? "" });
    }

    [HttpGet]
    public async Task<JsonResult> PrepareFromSalesOrder(int? id, CancellationToken ct = default)
    {
        if (id is not > 0)
            return Json(new { success = false, message = "Invalid order." });
        var o = await _db.SalesOrders.AsNoTracking()
            .Include(x => x.Items)!.ThenInclude(i => i.QuantityUom)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);
        if (o == null)
            return Json(new { success = false, message = "Sales order not found." });
        if (o.Status != SalesOrder.StatusConfirmed)
            return Json(new { success = false, message = "Only confirmed sales orders can create a delivery challan." });
        var giReceived = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .AnyAsync(g => g.SalesOrderId == o.Id && g.Status == SalesGoodsIssueDocument.StatusReceived, ct)
            .ConfigureAwait(false);
        if (!giReceived)
            return Json(new { success = false, message = "Sales goods issue must be received before delivery challan." });
        var hasDc0 = await _db.DeliveryChallans.AsNoTracking()
            .AnyAsync(d => d.SalesOrderId == o.Id, ct)
            .ConfigureAwait(false);
        if (hasDc0)
            return Json(new { success = false, message = "This order already has a delivery challan." });
        var doc = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var itemList = o.Items
            .OrderBy(i => i.Id)
            .Select(i => new
            {
                salesOrderItemId = i.Id,
                referenceSalesOrderNumber = o.SalesOrderNumber,
                materialNumber = i.MaterialNumber,
                materialDescription = i.MaterialDescription ?? "",
                deliveryQuantity = i.OrderQuantity,
                quantityUomId = i.QuantityUomId,
                uomCode = i.QuantityUom != null ? i.QuantityUom.Code : "",
                batch = ""
            })
            .ToList();
        return Json(new
        {
            success = true,
            plantId = o.PlantId,
            deliveryType = "Standard",
            shipToBusinessPartnerId = o.CustomerBusinessPartnerId,
            documentDate = doc,
            salesOrderId = o.Id,
            salesOrderNumber = o.SalesOrderNumber,
            shipToDisplayName = o.CustomerName ?? "",
            items = itemList
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeliveryChallanCreateFormModel model, CancellationToken ct = default)
    {
        var rows = (model.Items ?? new List<DeliveryChallanItemFormRow>())
            .Where(r => !string.IsNullOrWhiteSpace(r.MaterialNumber) && (r.DeliveryQuantity ?? 0) > 0)
            .ToList();
        if (rows.Count == 0)
        {
            TempData["DcError"] = "Add at least one line with a material and delivery quantity.";
            return RedirectToAction(nameof(Index));
        }
        if (string.IsNullOrWhiteSpace(model.PlantId))
        {
            TempData["DcError"] = "Select a plant.";
            return RedirectToAction(nameof(Index));
        }
        if (string.IsNullOrWhiteSpace(model.ShipToBusinessPartnerId))
        {
            TempData["DcError"] = "Select a ship-to party.";
            return RedirectToAction(nameof(Index));
        }
        foreach (var r in rows)
        {
            if (r.QuantityUomId is not > 0)
            {
                TempData["DcError"] = "Each line must have a unit of measure (UOM).";
                return RedirectToAction(nameof(Index));
            }
        }
        var docDate = (model.DocumentDate ?? DateTime.Today).Date;
        var delType = string.IsNullOrWhiteSpace(model.DeliveryType) ? "Standard" : model.DeliveryType.Trim();
        if (delType.Length > 64) delType = delType[..64];
        var shipName = await _db.BusinessPartnerMasterSamples.AsNoTracking()
            .Where(c => c.BPID == model.ShipToBusinessPartnerId)
            .Select(c => c.FullName)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        int? soId = model.SalesOrderId is int sid && sid > 0 ? sid : null;
        if (soId is int s)
        {
            var st = await _db.SalesOrders.AsNoTracking()
                .Where(x => x.Id == s)
                .Select(x => x.Status)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (string.IsNullOrEmpty(st))
                soId = null;
            else if (st != SalesOrder.StatusConfirmed)
            {
                TempData["DcError"] = "The linked sales order is not confirmed.";
                return RedirectToAction(nameof(Index));
            }
            var giReceived = await _db.SalesGoodsIssueDocuments.AsNoTracking()
                .AnyAsync(g => g.SalesOrderId == s && g.Status == SalesGoodsIssueDocument.StatusReceived, ct)
                .ConfigureAwait(false);
            if (!giReceived)
            {
                TempData["DcError"] = "Sales goods issue must be received before delivery challan.";
                return RedirectToAction(nameof(Index));
            }
        }
        if (soId is int dupSo)
        {
            var hasDup = await _db.DeliveryChallans.AnyAsync(d => d.SalesOrderId == dupSo, ct)
                .ConfigureAwait(false);
            if (hasDup)
            {
                TempData["DcError"] = "This sales order already has a delivery challan.";
                return RedirectToAction(nameof(Index));
            }
        }
        var refSoNum = (model.ReferenceSalesOrderNumber ?? "").Trim();
        if (string.IsNullOrEmpty(refSoNum) && soId is int s2)
        {
            refSoNum = await _db.SalesOrders.AsNoTracking()
                .Where(x => x.Id == s2)
                .Select(x => x.SalesOrderNumber)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false) ?? "";
        }
        var matNums = rows
            .Select(r => (r.MaterialNumber ?? "").Trim())
            .Where(m => m.Length > 0)
            .Distinct()
            .ToList();
        var descMap = await _db.CreateMaterialMaster.AsNoTracking()
            .Where(m => matNums.Contains(m.MaterialNumber))
            .ToDictionaryAsync(m => m.MaterialNumber, m => m.Description, ct)
            .ConfigureAwait(false);

        var lineBuild = new List<(string Mat, decimal Qty, int Uom, int? SItem, string? LineRef, string? MDesc)>();
        int? soIdForItem = soId;
        foreach (var r in rows)
        {
            var mat = (r.MaterialNumber ?? "").Trim();
            if (mat.Length == 0) continue;
            if (mat.Length > 32) mat = mat[..32];
            var qty = r.DeliveryQuantity.GetValueOrDefault();
            if (qty <= 0) continue;
            var uom = r.QuantityUomId!.Value;
            int? sItemId = r.SalesOrderItemId is int soi && soi > 0 ? soi : null;
            if (soIdForItem is not int s3)
                sItemId = null;
            else if (sItemId is int si2)
            {
                var soOk = await _db.SalesOrderItems.AnyAsync(i => i.Id == si2 && i.SalesOrderId == s3, ct)
                    .ConfigureAwait(false);
                if (!soOk) sItemId = null;
            }
            var lineRef = (r.ReferenceSalesOrderNumber ?? "").Trim();
            if (string.IsNullOrEmpty(lineRef)) lineRef = refSoNum;
            string? mdesc = null;
            if (descMap.TryGetValue(mat, out var d) && !string.IsNullOrEmpty(d))
                mdesc = d[..Math.Min(500, d.Length)];
            var qR = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
            lineBuild.Add((mat, qR, uom, sItemId, string.IsNullOrEmpty(lineRef) ? null : lineRef, mdesc));
        }

        if (lineBuild.Count == 0)
        {
            TempData["DcError"] = "Add at least one line with a material and delivery quantity.";
            return RedirectToAction(nameof(Index));
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
                    var dcNum = await _documentNumbers.AllocateAsync(ModuleKeys.DeliveryChallan, ct).ConfigureAwait(false);

                    var header = new DeliveryChallan
                    {
                        DeliveryChallanNumber = dcNum,
                        PlantId = model.PlantId,
                        DeliveryType = delType,
                        ShipToBusinessPartnerId = model.ShipToBusinessPartnerId,
                        ShipToDisplayName = string.IsNullOrWhiteSpace(shipName) ? null : shipName![..Math.Min(500, shipName.Length)],
                        DocumentDate = docDate,
                        SalesOrderId = soId,
                        ReferenceSalesOrderNumber = string.IsNullOrEmpty(refSoNum) ? null : refSoNum[..Math.Min(40, refSoNum.Length)]
                    };

                    foreach (var lb in lineBuild)
                    {
                        header.Items.Add(new DeliveryChallanItem
                        {
                            ReferenceSalesOrderNumber = lb.LineRef is { } lr ? lr[..Math.Min(40, lr.Length)] : null,
                            MaterialNumber = lb.Mat,
                            MaterialDescription = lb.MDesc,
                            DeliveryQuantity = lb.Qty,
                            QuantityUomId = lb.Uom,
                            Batch = null,
                            SalesOrderItemId = lb.SItem
                        });
                    }

                    await _db.DeliveryChallans.AddAsync(header, ct).ConfigureAwait(false);
                    await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                    var invNum = await _documentNumbers.AllocateAsync(ModuleKeys.Invoice, ct).ConfigureAwait(false);
                    await _invoiceFromDc.AddInvoiceForDeliveryChallanAsync(header, invNum, ct).ConfigureAwait(false);
                    await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                    await tx.CommitAsync(ct).ConfigureAwait(false);
                    TempData["DcMessage"] = $"Delivery challan {dcNum} created.";
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
            TempData["DcError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (DocumentIntegrationRangeExhaustedException ex)
        {
            TempData["DcError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["DcError"] = ex.InnerException?.Message ?? ex.Message;
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<DeliveryChallan?> LoadDeliveryChallanForPdfAsync(int id, CancellationToken ct) =>
        await _db.DeliveryChallans.AsNoTracking()
            .Include(x => x.Plant)
            .Include(x => x.SalesOrder)
            .Include(x => x.Items)!.ThenInclude(i => i.QuantityUom)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

    private static string SafePdfName(string number, string kind)
    {
        var s = (number ?? kind).Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '-');
        if (string.IsNullOrEmpty(s)) s = kind;
        if (!s.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) s += ".pdf";
        return s;
    }

}
