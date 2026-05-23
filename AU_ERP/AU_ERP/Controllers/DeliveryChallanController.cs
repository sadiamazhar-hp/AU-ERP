using System.Data;
using System.Globalization;
using System.IO;
using System.Security.Claims;
using AU_ERP.Configuration;
using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "SalesOrAdminDepartment")]
public class DeliveryChallanController : Controller
{
    private sealed class FleetSelectionUnavailableException(string message) : Exception(message);

    private readonly AppDbContext _db;
    private readonly DocumentNumberAllocator _documentNumbers;
    private readonly SalesInvoiceFromDeliveryChallanService _invoiceFromDc;
    private readonly CompanyInfoService _companyInfo;

    public DeliveryChallanController(
        AppDbContext db,
        DocumentNumberAllocator documentNumbers,
        SalesInvoiceFromDeliveryChallanService invoiceFromDc,
        CompanyInfoService companyInfo)
    {
        _db = db;
        _documentNumbers = documentNumbers;
        _invoiceFromDc = invoiceFromDc;
        _companyInfo = companyInfo;
    }

    private async Task<IReadOnlyList<string>> AllowedDeliveryChallanPlantIdsAsync(CancellationToken ct)
    {
        var all = await _db.PlantsSamples.AsNoTracking()
            .Select(p => p.PlantID)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return UserPlantResolution.GetDeliveryChallanPlantIdFilter(User, all).ToList();
    }

    private static string ExplainSalesOrderPlantDeniedForDc(ClaimsPrincipal user, SalesOrder o)
    {
        var p = (o.PlantId ?? "").Trim();
        if (string.IsNullOrEmpty(p))
            return "This sales order has no plant. Set the order’s plant before creating a delivery challan.";
        if (UserPlantResolution.IsAdminDepartment(user))
            return $"Sales order plant ‘{p}’ is not recognised for delivery challan (check Plants master and the order’s plant code).";
        var assigned = UserPlantResolution.GetStorePlantIds(user);
        if (assigned.Count > 0)
            return $"This order is for plant ‘{p}’, which is not in your assigned store plants ({string.Join(", ", assigned)}). An administrator can assign the matching store/plant in Users, or use an account allowed for that plant.";
        return $"Sales order plant ‘{p}’ is not recognised for delivery challan. Check Plants configuration and that the plant on the order matches a valid plant code.";
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
        var allPlantIds = plants.Select(p => p.PlantID).ToList();
        var allowedPlantIds = UserPlantResolution.GetDeliveryChallanPlantIdFilter(User, allPlantIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var dcModalPlants = plants.Where(p => allowedPlantIds.Contains(p.PlantID)).ToList();
        var storePlantIds = UserPlantResolution.GetStorePlantIds(User);
        var dcPlantSingleLocked = storePlantIds.Count == 1;
        var dcLockedPlantId = dcPlantSingleLocked ? storePlantIds[0] : null;
        var driversInUse = await DeliveryFleetAvailability.GetDriverIdsBusyOnOpenInvoiceAsync(_db, ct).ConfigureAwait(false);
        var vehiclesInUse = await DeliveryFleetAvailability.GetVehicleIdsBusyOnOpenInvoiceAsync(_db, ct).ConfigureAwait(false);

        var drivers = await _db.Drivers.AsNoTracking()
            .Where(d => d.IsActive && !driversInUse.Contains(d.Id))
            .OrderBy(d => d.LastName).ThenBy(d => d.FirstName).ThenBy(d => d.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var vehicles = await _db.Vehicles.AsNoTracking()
            .Where(v => v.IsActive && !vehiclesInUse.Contains(v.Id))
            .OrderBy(v => v.NumberPlate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        ViewBag.Plants = plants;
        ViewBag.DcModalPlants = dcModalPlants;
        ViewBag.DcPlantSingleLocked = dcPlantSingleLocked;
        ViewBag.DcLockedPlantId = dcLockedPlantId;
        ViewBag.DcDrivers = drivers;
        ViewBag.DcVehicles = vehicles;
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
            .Include(x => x.Driver)
            .Include(x => x.Vehicle)
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
            .Include(x => x.Driver)
            .Include(x => x.Vehicle)
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
        var eligibleGiSoIds = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .Where(g => g.DispatchStatus == SalesGoodsIssueDocument.DispatchSent
                || g.Status == SalesGoodsIssueDocument.StatusReceived)
            .Select(g => g.SalesOrderId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var eligibleGiSet = eligibleGiSoIds.ToHashSet();
        query = query.Where(x => eligibleGiSet.Contains(x.Id));
        if (withDcSet.Count > 0)
            query = query.Where(x => !withDcSet.Contains(x.Id));
        var t = (q ?? "").Trim();
        if (t.Length > 0)
        {
            query = query.Where(x =>
                x.SalesOrderNumber.Contains(t)
                || (x.CustomerName != null && x.CustomerName.Contains(t)));
        }
        var allPlantIds = await _db.PlantsSamples.AsNoTracking()
            .Select(p => p.PlantID)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var allowedPlants = UserPlantResolution.GetDeliveryChallanPlantIdFilter(User, allPlantIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        query = query.Where(x => x.PlantId != null && allowedPlants.Contains(x.PlantId));
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
        var allPlantIdsLines = await _db.PlantsSamples.AsNoTracking()
            .Select(p => p.PlantID)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var allowedLines = UserPlantResolution.GetDeliveryChallanPlantIdFilter(User, allPlantIdsLines)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(o.PlantId) || !allowedLines.Contains(o.PlantId.Trim()))
            return Json(new { success = false, message = ExplainSalesOrderPlantDeniedForDc(User, o) });
        if (o.Status != SalesOrder.StatusConfirmed)
            return Json(new { success = false, message = "Only confirmed sales orders are listed." });
        var giDispatchedLines = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .AnyAsync(g => g.SalesOrderId == o.Id
                && (g.DispatchStatus == SalesGoodsIssueDocument.DispatchSent
                    || g.Status == SalesGoodsIssueDocument.StatusReceived), ct)
            .ConfigureAwait(false);
        if (!giDispatchedLines)
            return Json(new { success = false, message = "Sales goods issue must be dispatched (sent) or received before delivery challan." });
        var batchMapLines = await LoadLatestGiBatchBySalesOrderItemAsync(o.Id, ct).ConfigureAwait(false);
        var lines = o.Items
            .OrderBy(i => i.Id)
            .Select(i => new
            {
                salesOrderItemId = i.Id,
                materialNumber = i.MaterialNumber,
                materialDescription = i.MaterialDescription ?? "",
                orderQuantity = i.OrderQuantity,
                quantityUomId = i.QuantityUomId,
                uomCode = i.QuantityUom != null ? i.QuantityUom.Code : "",
                batch = batchMapLines.TryGetValue(i.Id, out var bLine) ? bLine : ""
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
        var companyHeader = await _companyInfo.GetPdfHeaderAsync(ct).ConfigureAwait(false);
        var bytes = DeliveryChallanPdfService.BuildPdf(d, d.Items.ToList(), companyHeader);
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
        var allPlantIdsPrep = await _db.PlantsSamples.AsNoTracking()
            .Select(p => p.PlantID)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var allowedPrep = UserPlantResolution.GetDeliveryChallanPlantIdFilter(User, allPlantIdsPrep)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(o.PlantId) || !allowedPrep.Contains(o.PlantId.Trim()))
            return Json(new { success = false, message = ExplainSalesOrderPlantDeniedForDc(User, o) });
        if (o.Status != SalesOrder.StatusConfirmed)
            return Json(new { success = false, message = "Only confirmed sales orders can create a delivery challan." });
        var giDispatchedPrep = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .AnyAsync(g => g.SalesOrderId == o.Id
                && (g.DispatchStatus == SalesGoodsIssueDocument.DispatchSent
                    || g.Status == SalesGoodsIssueDocument.StatusReceived), ct)
            .ConfigureAwait(false);
        if (!giDispatchedPrep)
            return Json(new { success = false, message = "Sales goods issue must be dispatched (sent) or received before delivery challan." });
        var hasDc0 = await _db.DeliveryChallans.AsNoTracking()
            .AnyAsync(d => d.SalesOrderId == o.Id, ct)
            .ConfigureAwait(false);
        if (hasDc0)
            return Json(new { success = false, message = "This order already has a delivery challan." });
        var doc = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var batchMapPrep = await LoadLatestGiBatchBySalesOrderItemAsync(o.Id, ct).ConfigureAwait(false);
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
                batch = batchMapPrep.TryGetValue(i.Id, out var bIt) ? bIt : ""
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
        if (model.SalesOrderId is not > 0)
        {
            TempData["DcError"] = "Select a sales order for this delivery challan.";
            return RedirectToAction(nameof(Index));
        }
        if (string.IsNullOrWhiteSpace(model.PlantId))
        {
            TempData["DcError"] = "Select a plant.";
            return RedirectToAction(nameof(Index));
        }
        var allPlantIdsForUser = await _db.PlantsSamples.AsNoTracking()
            .Select(p => p.PlantID)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var allowedPlants = UserPlantResolution.GetDeliveryChallanPlantIdFilter(User, allPlantIdsForUser)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!allowedPlants.Contains(model.PlantId.Trim()))
        {
            TempData["DcError"] = "Selected plant is not allowed for your user.";
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
            var soRow = await _db.SalesOrders.AsNoTracking()
                .Where(x => x.Id == s)
                .Select(x => new { x.Status, x.PlantId })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (soRow == null)
            {
                TempData["DcError"] = "Linked sales order was not found.";
                return RedirectToAction(nameof(Index));
            }
            if (soRow.Status != SalesOrder.StatusConfirmed)
            {
                TempData["DcError"] = "The linked sales order is not confirmed.";
                return RedirectToAction(nameof(Index));
            }
            if (!string.Equals((soRow.PlantId ?? "").Trim(), (model.PlantId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["DcError"] = "Sales order plant must match the challan plant.";
                return RedirectToAction(nameof(Index));
            }
            var giDispatchedCreate = await _db.SalesGoodsIssueDocuments.AsNoTracking()
                .AnyAsync(g => g.SalesOrderId == s
                    && (g.DispatchStatus == SalesGoodsIssueDocument.DispatchSent
                        || g.Status == SalesGoodsIssueDocument.StatusReceived), ct)
                .ConfigureAwait(false);
            if (!giDispatchedCreate)
            {
                TempData["DcError"] = "Sales goods issue must be dispatched (sent) or received before delivery challan.";
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

        var batchBySoItemId = soId is int soIntForBatch
            ? await LoadLatestGiBatchBySalesOrderItemAsync(soIntForBatch, ct).ConfigureAwait(false)
            : new Dictionary<int, string>();

        var strategy = _db.Database.CreateExecutionStrategy();
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                    .ConfigureAwait(false);
                try
                {
                    int? driverId = model.DriverId is > 0 ? model.DriverId : null;
                    int? vehicleId = model.VehicleId is > 0 ? model.VehicleId : null;

                    var driversInUseTx = await DeliveryFleetAvailability.GetDriverIdsBusyOnOpenInvoiceAsync(_db, ct).ConfigureAwait(false);
                    var vehiclesInUseTx = await DeliveryFleetAvailability.GetVehicleIdsBusyOnOpenInvoiceAsync(_db, ct).ConfigureAwait(false);

                    if (driverId is int did)
                    {
                        var okD = await _db.Drivers.AsNoTracking()
                            .AnyAsync(x => x.Id == did && x.IsActive, ct)
                            .ConfigureAwait(false);
                        if (!okD)
                            throw new FleetSelectionUnavailableException("Selected driver is invalid or inactive.");
                        if (driversInUseTx.Contains(did))
                            throw new FleetSelectionUnavailableException(
                                "Selected driver is not available (already on another active delivery).");
                    }
                    if (vehicleId is int vid)
                    {
                        var okV = await _db.Vehicles.AsNoTracking()
                            .AnyAsync(x => x.Id == vid && x.IsActive, ct)
                            .ConfigureAwait(false);
                        if (!okV)
                            throw new FleetSelectionUnavailableException("Selected vehicle is invalid or inactive.");
                        if (vehiclesInUseTx.Contains(vid))
                            throw new FleetSelectionUnavailableException(
                                "Selected vehicle is not available (already on another active delivery).");
                    }

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
                        ReferenceSalesOrderNumber = string.IsNullOrEmpty(refSoNum) ? null : refSoNum[..Math.Min(40, refSoNum.Length)],
                        DriverId = driverId,
                        VehicleId = vehicleId
                    };

                    foreach (var lb in lineBuild)
                    {
                        string? batchCell = null;
                        if (lb.SItem is int sItemKey && batchBySoItemId.TryGetValue(sItemKey, out var bsSum))
                        {
                            var t = bsSum.Trim();
                            if (t.Length > 0)
                                batchCell = t.Length <= 64 ? t : t[..64];
                        }
                        header.Items.Add(new DeliveryChallanItem
                        {
                            ReferenceSalesOrderNumber = lb.LineRef is { } lr ? lr[..Math.Min(40, lr.Length)] : null,
                            MaterialNumber = lb.Mat,
                            MaterialDescription = lb.MDesc,
                            DeliveryQuantity = lb.Qty,
                            QuantityUomId = lb.Uom,
                            Batch = batchCell,
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
        catch (FleetSelectionUnavailableException ex)
        {
            TempData["DcError"] = ex.Message;
            return RedirectToAction(nameof(Index));
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDeliveryCompleted(
        int id,
        string? returnTo = null,
        string? returnQ = null,
        string? returnStatus = null,
        string? returnPlantId = null,
        int? returnDistributionChannelId = null,
        string? returnDateFrom = null,
        string? returnDateTo = null,
        CancellationToken ct = default)
    {
        var dc = await _db.DeliveryChallans.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (dc == null)
            return NotFound();
        var allowed = await AllowedDeliveryChallanPlantIdsAsync(ct).ConfigureAwait(false);
        var pid = (dc.PlantId ?? "").Trim();
        if (pid.Length > 0
            && !allowed.Any(a => string.Equals(a, pid, StringComparison.OrdinalIgnoreCase)))
        {
            SetDeliveryCompletedFlash(false, "You cannot change this delivery challan (plant not allowed for your login).", returnTo);
            return RedirectAfterDeliveryCompleted(returnTo, returnQ, returnStatus, returnPlantId, returnDistributionChannelId, returnDateFrom, returnDateTo);
        }

        if (dc.DeliveryCompletedAt != null)
        {
            SetDeliveryCompletedFlash(true, $"Delivery challan {dc.DeliveryChallanNumber} is already marked completed.", returnTo);
            return RedirectAfterDeliveryCompleted(returnTo, returnQ, returnStatus, returnPlantId, returnDistributionChannelId, returnDateFrom, returnDateTo);
        }

        if (dc.DriverId == null && dc.VehicleId == null)
        {
            SetDeliveryCompletedFlash(false,
                "Assign a driver or vehicle on this delivery challan before marking delivery completed.",
                returnTo);
            return RedirectAfterDeliveryCompleted(returnTo, returnQ, returnStatus, returnPlantId, returnDistributionChannelId, returnDateFrom, returnDateTo);
        }

        dc.DeliveryCompletedAt = DateTime.UtcNow;
        dc.DeliveryCompletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        SetDeliveryCompletedFlash(true,
            $"Marked delivery completed for {dc.DeliveryChallanNumber}. Driver / vehicle are available for new challans.",
            returnTo);
        return RedirectAfterDeliveryCompleted(returnTo, returnQ, returnStatus, returnPlantId, returnDistributionChannelId, returnDateFrom, returnDateTo);
    }

    private void SetDeliveryCompletedFlash(bool isWarning, string message, string? returnTo)
    {
        if (string.Equals((returnTo ?? "").Trim(), "salesorder", StringComparison.OrdinalIgnoreCase))
        {
            if (isWarning) TempData["OrderMessage"] = message;
            else TempData["OrderError"] = message;
            return;
        }
        if (string.Equals((returnTo ?? "").Trim(), "returnorder", StringComparison.OrdinalIgnoreCase))
        {
            if (isWarning) TempData["RoMessage"] = message;
            else TempData["RoError"] = message;
            return;
        }
        if (isWarning) TempData["DcWarning"] = message;
        else TempData["DcError"] = message;
    }

    private IActionResult RedirectAfterDeliveryCompleted(
        string? returnTo,
        string? returnQ,
        string? returnStatus,
        string? returnPlantId,
        int? returnDistributionChannelId,
        string? returnDateFrom = null,
        string? returnDateTo = null)
    {
        var dest = (returnTo ?? "").Trim();
        if (string.Equals(dest, "salesorder", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "SalesOrder", new
            {
                q = returnQ,
                status = returnStatus,
                plantId = returnPlantId,
                distributionChannelId = returnDistributionChannelId is > 0 ? returnDistributionChannelId : null
            });
        }
        if (string.Equals(dest, "returnorder", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "ReturnOrder", new
            {
                q = returnQ,
                status = returnStatus,
                dateFrom = string.IsNullOrWhiteSpace(returnDateFrom) ? null : returnDateFrom,
                dateTo = string.IsNullOrWhiteSpace(returnDateTo) ? null : returnDateTo
            });
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

    /// <summary>Latest sales goods issue line batch text keyed by sales order item id (from dispatch / send).</summary>
    private async Task<Dictionary<int, string>> LoadLatestGiBatchBySalesOrderItemAsync(int salesOrderId, CancellationToken ct)
    {
        var map = new Dictionary<int, string>();
        var gi = await _db.SalesGoodsIssueDocuments.AsNoTracking()
            .Include(g => g.Lines)
            .Where(g => g.SalesOrderId == salesOrderId)
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (gi?.Lines == null)
            return map;
        foreach (var gl in gi.Lines)
        {
            if (gl.SalesOrderItemId is not > 0 || string.IsNullOrWhiteSpace(gl.BatchSummary))
                continue;
            var key = gl.SalesOrderItemId.Value;
            if (!map.ContainsKey(key))
                map[key] = gl.BatchSummary.Trim();
        }
        return map;
    }

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
