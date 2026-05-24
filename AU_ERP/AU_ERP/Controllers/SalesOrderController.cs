using System.Globalization;
using System.IO;
using System.Security.Claims;
using AU_ERP.Configuration;
using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using AU_ERP.Validation;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "SalesDepartment")]
public class SalesOrderController : Controller
{
    private readonly AppDbContext _db;
    private readonly SalesGoodsIssueService _salesGiService;
    private readonly DocumentNumberAllocator _documentNumbers;
    private readonly CompanyInfoService _companyInfo;
    private readonly EmporiumWalkInCustomerService _emporiumWalkIn;
    private readonly SalesOrderWorkflowStatusResolver _workflowStatus;

    public SalesOrderController(
        AppDbContext db,
        SalesGoodsIssueService salesGiService,
        DocumentNumberAllocator documentNumbers,
        CompanyInfoService companyInfo,
        EmporiumWalkInCustomerService emporiumWalkIn,
        SalesOrderWorkflowStatusResolver workflowStatus)
    {
        _db = db;
        _salesGiService = salesGiService;
        _documentNumbers = documentNumbers;
        _companyInfo = companyInfo;
        _emporiumWalkIn = emporiumWalkIn;
        _workflowStatus = workflowStatus;
    }

    /// <summary>FERT materials for quotation line picker (same JSON shape as BOM material search).</summary>
    [HttpGet]
    public async Task<JsonResult> SearchFertMaterialsForSalesOrder(string? q, CancellationToken ct = default)
    {
        IQueryable<CreateMaterialMaster> query = _db.CreateMaterialMaster.AsNoTracking()
            .Where(m => m.MaterialTypeCode == "FERT");
        var qq = (q ?? "").Trim();
        if (qq.Length > 0)
            query = query.Where(m =>
                m.MaterialNumber.Contains(qq) ||
                (m.Description != null && m.Description.Contains(qq)));
        var mats = await query
            .OrderBy(m => m.MaterialNumber)
            .Take(50)
            .Select(m => new
            {
                m.MaterialNumber,
                Description = m.Description ?? "",
                m.MaterialTypeCode,
                m.BaseUnitCode
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var codes = mats
            .Where(m => !string.IsNullOrEmpty(m.BaseUnitCode))
            .Select(m => m.BaseUnitCode!)
            .Distinct()
            .ToList();
        var uomMap = await _db.UnitOfMeasurements.AsNoTracking()
            .Where(u => u.Code != null && codes.Contains(u.Code))
            .ToDictionaryAsync(u => u.Code!, u => u.Id, ct)
            .ConfigureAwait(false);
        var items = mats.Select(m => new
        {
            n = m.MaterialNumber,
            d = m.Description,
            t = m.MaterialTypeCode,
            baseUom = m.BaseUnitCode ?? "",
            uomId = m.BaseUnitCode != null && uomMap.TryGetValue(m.BaseUnitCode, out var uid) ? (int?)uid : null
        }).ToList();
        return Json(new { success = true, items });
    }

    [HttpGet]
    public async Task<JsonResult> CustomerCommercialProfile(string? bpId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bpId))
            return Json(new { success = false, message = "Customer is required." });

        var bp = await _db.BusinessPartnerMasterSamples.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BPID == bpId.Trim(), ct)
            .ConfigureAwait(false);
        if (bp == null)
            return Json(new { success = false, message = "Customer not found." });

        var schemaErr = await ValidateCustomerSalesSchemaAsync(bp, plantId: null, forSalesOrder: true, ct).ConfigureAwait(false);
        if (schemaErr != null)
            return Json(new { success = false, message = schemaErr });

        var channelId = await ResolveDistributionChannelIdAsync(bp.DistChannel, ct).ConfigureAwait(false);
        var salesSchemaId = await ResolveSalesSchemaIdAsync(bp.SalesSchema, ct).ConfigureAwait(false);
        var plantId = await ResolvePlantIdFromSalesSchemaAsync(bp.SalesSchema, ct).ConfigureAwait(false);
        var addr = BuildBpShipTo(bp);

        return Json(new
        {
            success = true,
            profile = new
            {
                salesSchema = bp.SalesSchema,
                distributionChannel = bp.DistChannel,
                distributionChannelId = channelId,
                configurationSchemaId = salesSchemaId,
                plantId,
                shipToAddress = addr
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<JsonResult> CreateWalkInCustomer(
        [FromForm] string? firstName,
        [FromForm] string? lastName,
        [FromForm] string? address,
        [FromForm] string? mobile,
        CancellationToken ct = default)
    {
        var (ok, err, bpId, displayName) = await _emporiumWalkIn
            .CreateWalkInCustomerAsync(User, firstName, lastName, address, mobile, ct)
            .ConfigureAwait(false);
        if (!ok)
            return Json(new { success = false, message = err ?? "Could not create customer." });
        return Json(new { success = true, bpId, displayName });
    }

    [HttpGet]
    public async Task<JsonResult> AvailableSalesQuotations(int? currentOrderId, CancellationToken ct = default)
    {
        var quotedOrderBySq = await _db.SalesOrders.AsNoTracking()
            .Where(o => o.SalesQuotationId != null && (currentOrderId == null || o.Id != currentOrderId.Value))
            .Select(o => o.SalesQuotationId!.Value)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var used = quotedOrderBySq.ToHashSet();
        var list = await _db.SalesQuotations.AsNoTracking()
            .Where(q => q.Status == SalesQuotation.StatusSent && !used.Contains(q.Id))
            .OrderByDescending(q => q.QuotationDate)
            .ThenBy(q => q.QuotationNumber)
            .Select(q => new { q.Id, q.QuotationNumber, q.CustomerName })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return Json(new { success = true, items = list });
    }

    [HttpGet]
    public async Task<JsonResult> SalesQuotationItems(int quotationId, CancellationToken ct = default)
    {
        if (quotationId <= 0)
            return Json(new { success = false, message = "Sales quotation is required." });
        var q = await _db.SalesQuotations.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == quotationId, ct)
            .ConfigureAwait(false);
        if (q == null)
            return Json(new { success = false, message = "Quotation not found." });
        if (q.Status != SalesQuotation.StatusSent)
            return Json(new { success = false, message = "Only sent quotations can be selected." });

        return Json(new
        {
            success = true,
            header = new
            {
                q.Id,
                q.QuotationNumber,
                q.PlantId,
                q.DistributionChannelId,
                q.ConfigurationSchemaId,
                q.CustomerBusinessPartnerId,
                q.CustomerName,
                q.ShipToAddress,
                q.ItemChargeColumnIds,
                q.QuotationLevelChargeIds,
                q.QuotationChargeValuesJson
            },
            items = q.Items.OrderBy(i => i.Id).Select(i => new
            {
                i.MaterialNumber,
                i.MaterialDescription,
                i.QuantityUomId,
                i.SalesPriceGrade,
                orderQuantity = i.OrderQuantity,
                i.UnitPrice,
                i.ItemChargeValuesJson,
                deliveryDate = i.DeliveryDate != null ? i.DeliveryDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : ""
            }).ToList()
        });
    }

    [HttpGet]
    public async Task<JsonResult> NextSalesOrderNumber(CancellationToken ct = default)
    {
        try
        {
            var n = await _documentNumbers.PeekNextAsync(ModuleKeys.SaleOrder, ct).ConfigureAwait(false);
            return Json(new { success = true, number = n, display = n });
        }
        catch (DocumentIntegrationMissingException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (DocumentIntegrationRangeExhaustedException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<JsonResult> SchemaDetails(int? id, CancellationToken ct = default)
    {
        if (id is not > 0)
            return Json(new { success = false, message = "Schema is required." });
        var list = await _db.ConfigurationSchemaCharges.AsNoTracking()
            .Where(x => x.ConfigurationSchemaId == id)
            .Join(_db.Charges.AsNoTracking(), c => c.ChargeId, ch => ch.Id, (_, ch) => ch)
            .OrderBy(ch => ch.Symbol)
            .Select(ch => new
            {
                id = ch.Id,
                symbol = ch.Symbol,
                description = ch.Description,
                valueType = ch.ValueType,
                sign = ch.Sign,
                defaultPercent = ch.DefaultPercent
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return Json(new { success = true, charges = list });
    }

    /// <summary>All defined charges for quotation line/summary pickers (no configuration schema on the form).</summary>
    [HttpGet]
    public async Task<JsonResult> AllCharges(CancellationToken ct = default)
    {
        var list = await _db.Charges.AsNoTracking()
            .OrderBy(ch => ch.Symbol)
            .Select(ch => new
            {
                id = ch.Id,
                symbol = ch.Symbol,
                description = ch.Description,
                valueType = ch.ValueType,
                sign = ch.Sign,
                defaultPercent = ch.DefaultPercent
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return Json(new { success = true, charges = list });
    }

    [HttpGet]
    public async Task<JsonResult> MaterialUomContextForSalesOrder(string? materialNumber, int? includeUomIdForEdit, CancellationToken ct = default)
    {
        var (success, errorMessage, data) =
            await MaterialUomForMaterialHelper.TryBuildMaterialUomContextAsync(_db, materialNumber, includeUomIdForEdit, ct);
        if (!success)
            return Json(new { success = false, message = errorMessage });
        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<JsonResult> SalesOrderLineUnitCost(string? materialNumber, int? uomId, string? priceGrade, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(materialNumber) || uomId is not > 0)
            return Json(new { success = false, message = "Material and UOM are required." });
        var g = (priceGrade ?? "").Trim();
        if (string.IsNullOrEmpty(g)) g = StockInventoryGradeCodes.FirstQuality;
        var std = await InventoryStandardCostService.ResolveStandardCostPerUomAsync(
                _db, materialNumber.Trim(), uomId.Value, ct, g)
            .ConfigureAwait(false);
        return Json(new { success = true, stdCostPerUom = std });
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? editId,
        string? q,
        string? status,
        string? plantId,
        int? distributionChannelId,
        CancellationToken ct = default)
    {
        if (editId is > 0)
            ViewBag.OpenEditId = editId;
        var plants = await _db.PlantsSamples.AsNoTracking()
            .OrderBy(p => p.PlantName)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var channels = await _db.DistributionChannels.AsNoTracking()
            .OrderBy(c => c.DistributionChannelName)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        IQueryable<SalesOrder> query = _db.SalesOrders.AsNoTracking()
            .Include(so => so.Plant)
            .Include(so => so.DistributionChannel)
            .Include(so => so.SalesQuotation);

        var qq = (q ?? "").Trim();
        if (qq.Length > 0)
        {
            query = query.Where(x =>
                x.SalesOrderNumber.Contains(qq)
                || (x.CustomerName != null && x.CustomerName.Contains(qq)));
        }

        if (!string.IsNullOrWhiteSpace(plantId))
            query = query.Where(x => x.PlantId == plantId);
        if (distributionChannelId is { } dcid && dcid > 0)
            query = query.Where(x => x.DistributionChannelId == dcid);

        var allMatching = await query
            .OrderByDescending(so => so.OrderDate)
            .ThenBy(so => so.SalesOrderNumber)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var st = (status ?? "All").Trim();
        var countFilter = new SalesOrderWorkflowCountFilter
        {
            Q = string.IsNullOrEmpty(qq) ? null : qq,
            PlantId = string.IsNullOrWhiteSpace(plantId) ? null : plantId,
            DistributionChannelId = distributionChannelId is > 0 ? distributionChannelId : null
        };
        var statusCounts = await _workflowStatus.CountByStatusAsync(countFilter, ct).ConfigureAwait(false);

        var workflowById = await _workflowStatus.ResolveBatchAsync(
            allMatching.Select(x => x.Id).ToList(),
            ct).ConfigureAwait(false);

        var list = FilterByWorkflowStatus(allMatching, workflowById, st);
        var walkInSchemaId = await SalesSchemaResolution.GetWalkInSchemaIdAsync(_db, ct).ConfigureAwait(false);
        var dealerSchemaId = await SalesSchemaResolution.GetDealerSchemaIdAsync(_db, ct).ConfigureAwait(false);
        var isEmporiumWalkIn = UserPlantResolution.HasEmporiumStorePlant(User) && walkInSchemaId is > 0;
        var customersQuery = _db.BusinessPartnerMasterSamples.AsNoTracking();
        if (isEmporiumWalkIn)
            customersQuery = SalesSchemaResolution.FilterWalkInCustomers(customersQuery, walkInSchemaId!.Value);
        else if (dealerSchemaId is > 0)
            customersQuery = SalesSchemaResolution.FilterDealerCustomers(customersQuery, dealerSchemaId.Value);
        var customers = await customersQuery
            .OrderBy(c => c.FullName)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var schemas = await _db.ConfigurationSchemas.AsNoTracking()
            .Where(s => s.SchemaType == ConfigurationSchemaType.Sales)
            .OrderBy(s => s.Title)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        ViewBag.Plants = plants;
        ViewBag.DistributionChannels = channels;
        ViewBag.Customers = customers;
        ViewBag.ConfigurationSchemas = schemas;
        ViewBag.WalkInSchemaId = walkInSchemaId ?? 0;
        ViewBag.IsEmporiumWalkInUser = isEmporiumWalkIn;
        var orderIds = list.Select(x => x.Id).ToList();
        var dcRows = orderIds.Count == 0
            ? new List<DeliveryChallan>()
            : await _db.DeliveryChallans.AsNoTracking()
                .Where(d => d.SalesOrderId != null && orderIds.Contains(d.SalesOrderId.Value))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        var withDcIds = dcRows.Select(d => d.SalesOrderId!.Value).Distinct().ToList();
        var dcBySoId = dcRows
            .GroupBy(d => d.SalesOrderId!.Value)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var d = g.OrderByDescending(x => x.Id).First();
                    return new DeliveryChallanFleetActionVm
                    {
                        DeliveryChallanId = d.Id,
                        DeliveryChallanNumber = d.DeliveryChallanNumber,
                        IsDeliveryCompleted = d.DeliveryCompletedAt.HasValue,
                        CanMarkDeliveryCompleted = !d.DeliveryCompletedAt.HasValue
                            && (d.DriverId != null || d.VehicleId != null)
                    };
                });
        List<SalesGoodsIssueDocument> giRows;
        try
        {
            giRows = await _db.SalesGoodsIssueDocuments.AsNoTracking()
                .Where(g => orderIds.Contains(g.SalesOrderId))
                .GroupBy(g => g.SalesOrderId)
                .Select(gr => gr.OrderByDescending(x => x.Id).First())
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
        catch (SqlException ex) when (
            ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) &&
            ex.Message.Contains("SalesGoodsIssueDocuments", StringComparison.OrdinalIgnoreCase))
        {
            TempData["OrderError"] = "Sales GI tables are missing in the current database. Run database migrations for this environment.";
            giRows = new List<SalesGoodsIssueDocument>();
        }
        var vm = new SalesOrderListVm
        {
            Items = list,
            Q = string.IsNullOrEmpty(qq) ? null : qq,
            Status = string.IsNullOrEmpty(st) ? "All" : st,
            PlantId = string.IsNullOrWhiteSpace(plantId) ? null : plantId,
            DistributionChannelId = distributionChannelId is > 0 ? distributionChannelId : null,
            WorkflowStatusById = workflowById,
            CountOpen = statusCounts.GetValueOrDefault(SalesOrderWorkflowStatus.Open),
            CountPendingStock = statusCounts.GetValueOrDefault(SalesOrderWorkflowStatus.PendingStock),
            CountPendingGoodReceive = statusCounts.GetValueOrDefault(SalesOrderWorkflowStatus.PendingGoodReceive),
            CountPendingDc = statusCounts.GetValueOrDefault(SalesOrderWorkflowStatus.PendingDc),
            CountDeliveryInProcess = statusCounts.GetValueOrDefault(SalesOrderWorkflowStatus.DeliveryInProcess),
            CountPendingPayment = statusCounts.GetValueOrDefault(SalesOrderWorkflowStatus.PendingPayment),
            CountCompleted = statusCounts.GetValueOrDefault(SalesOrderWorkflowStatus.Completed),
            SalesOrderIdsWithChallan = withDcIds.ToHashSet(),
            SalesOrderGiStatusById = giRows.ToDictionary(x => x.SalesOrderId, x => x.Status),
            SalesOrderGiDocIdByOrderId = giRows.ToDictionary(x => x.SalesOrderId, x => x.Id),
            SalesOrderGiDocumentNumberById = giRows.ToDictionary(x => x.SalesOrderId, x => x.DocumentNumber),
            SalesOrderGiDispatchStatusById = giRows.ToDictionary(x => x.SalesOrderId, x => x.DispatchStatus),
            DeliveryChallanBySalesOrderId = dcBySoId
        };
        return View(vm);
    }

    private static List<SalesOrder> FilterByWorkflowStatus(
        List<SalesOrder> orders,
        IReadOnlyDictionary<int, string> workflowById,
        string statusFilter)
    {
        if (string.IsNullOrWhiteSpace(statusFilter)
            || string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
            return orders;

        if (string.Equals(statusFilter, SalesOrder.StatusConfirmed, StringComparison.OrdinalIgnoreCase))
        {
            return orders.Where(o =>
                workflowById.TryGetValue(o.Id, out var ws)
                && !string.Equals(ws, SalesOrderWorkflowStatus.Open, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return orders.Where(o =>
            workflowById.TryGetValue(o.Id, out var ws)
            && string.Equals(ws, statusFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken ct = default)
    {
        var o = await LoadSalesOrderForPdfAsync(id, ct).ConfigureAwait(false);
        if (o == null)
            return NotFound();
        var companyHeader = await _companyInfo.GetPdfHeaderAsync(ct).ConfigureAwait(false);
        var bytes = SalesOrderPdfService.BuildPdf(o, o.Items.OrderBy(i => i.Id).ToList(), companyHeader);
        var fileName = SafeSalesOrderPdfName(o.SalesOrderNumber);
        return File(bytes, "application/pdf", fileName);
    }

    /// <summary>Read-only details for list “View” modal (confirmed orders only; no layout).</summary>
    [HttpGet]
    public async Task<IActionResult> DetailsModal(int id, CancellationToken ct = default)
    {
        var o = await _db.SalesOrders.AsNoTracking()
            .Include(x => x.Plant)
            .Include(x => x.DistributionChannel)
            .Include(x => x.ConfigurationSchema)
            .Include(x => x.SalesQuotation)
            .Include(x => x.Items)!.ThenInclude(i => i.QuantityUom)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);
        if (o == null)
            return NotFound();
        if (o.Status != SalesOrder.StatusConfirmed)
            return NotFound();
        var full = await SalesDocumentDetailsModalBuilder.BuildSalesOrderAsync(o, _db, ct).ConfigureAwait(false);
        var workflowStatus = await _workflowStatus.ResolveAsync(id, ct).ConfigureAwait(false);
        full = new SalesOrderDetailsFullVm
        {
            Order = full.Order,
            Pricing = full.Pricing,
            WorkflowStatus = workflowStatus
        };
        return PartialView("_DetailsModal", full);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesOrderCreateFormModel model, CancellationToken ct = default, int? updateOrderId = null)
    {
        var isDraftSubmit = string.Equals(model.SubmitAction, "draft", StringComparison.OrdinalIgnoreCase);
        var isConfirmSubmit = string.Equals(model.SubmitAction, "send", StringComparison.OrdinalIgnoreCase);
        var strict = isConfirmSubmit;

        if (strict)
        {
            if (!model.OrderDate.HasValue || !model.RequestedDeliveryDate.HasValue)
            {
                TempData["OrderError"] = "This action requires order date and requested delivery date.";
                return RedirectToOrderListFromModel(model);
            }
        }

        var now = DateTime.Today;
        var orderDay = (model.OrderDate?.Date) ?? now;
        var reqDel = (model.RequestedDeliveryDate?.Date) ?? now;
        if (strict && reqDel < orderDay)
        {
            TempData["OrderError"] = "Requested delivery must be on or after order date.";
            return RedirectToOrderListFromModel(model);
        }

        string? customerName = null;
        string? customerShipTo = null;
        if (!string.IsNullOrWhiteSpace(model.CustomerBusinessPartnerId))
        {
            var bp = await _db.BusinessPartnerMasterSamples.AsNoTracking()
                .FirstOrDefaultAsync(c => c.BPID == model.CustomerBusinessPartnerId, ct)
                .ConfigureAwait(false);
            if (bp == null)
            {
                TempData["OrderError"] = "Invalid customer.";
                return RedirectToOrderListFromModel(model);
            }
            customerName = (bp.FullName ?? "").Trim();
            customerShipTo = BuildBpShipTo(bp);
            await ApplyCustomerDrivenDefaultsAsync(model, bp, ct).ConfigureAwait(false);
            var schemaErr = await ValidateCustomerSalesSchemaAsync(bp, model.PlantId, forSalesOrder: true, ct).ConfigureAwait(false);
            if (schemaErr != null)
            {
                TempData["OrderError"] = schemaErr;
                return RedirectToOrderListFromModel(model);
            }
        }
        else if (strict)
        {
            TempData["OrderError"] = "Select a customer.";
            return RedirectToOrderListFromModel(model);
        }

        var shipTo = (model.ShipToAddress ?? "").Trim();
        if (string.IsNullOrWhiteSpace(shipTo) && !string.IsNullOrWhiteSpace(customerShipTo))
            shipTo = customerShipTo;

        if (model.SalesQuotationId is int quoteId && quoteId > 0)
        {
            var quoteOrder = await _db.SalesOrders.AsNoTracking()
                .Where(o => o.SalesQuotationId == quoteId && (updateOrderId == null || o.Id != updateOrderId.Value))
                .Select(o => o.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (quoteOrder > 0)
            {
                TempData["OrderError"] = "Selected sales quotation is already linked to another sales order.";
                return RedirectToOrderListFromModel(model);
            }
        }
        if (strict && string.IsNullOrWhiteSpace(shipTo))
        {
            TempData["OrderError"] = "Confirm order requires a ship-to address (enter or fill from customer).";
            return RedirectToOrderListFromModel(model);
        }
        if (strict && (string.IsNullOrWhiteSpace(model.PlantId)
            || !model.DistributionChannelId.HasValue
            || !model.ConfigurationSchemaId.HasValue
            || model.ConfigurationSchemaId is not > 0))
        {
            TempData["OrderError"] = "This action requires plant, distribution channel, and configuration schema.";
            return RedirectToOrderListFromModel(model);
        }
        if (strict && model.ConfigurationSchemaId is int strictSchemaId)
        {
            var schemaOk = await _db.ConfigurationSchemas.AsNoTracking()
                .AnyAsync(s => s.Id == strictSchemaId && s.SchemaType == ConfigurationSchemaType.Sales, ct)
                .ConfigureAwait(false);
            if (!schemaOk)
            {
                TempData["OrderError"] = "Invalid configuration schema.";
                return RedirectToOrderListFromModel(model);
            }
        }

        IReadOnlyList<int> schemaChargeIds = Array.Empty<int>();
        if (model.ConfigurationSchemaId is int schId2 && schId2 > 0)
        {
            schemaChargeIds = await _db.ConfigurationSchemaCharges.AsNoTracking()
                .Where(x => x.ConfigurationSchemaId == schId2)
                .Select(x => x.ChargeId)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
        var schemaSet = new HashSet<int>(schemaChargeIds);
        var itemColList = SalesQuotationPricing.ParseIdList(model.ItemChargeColumnIds);
        foreach (var cid in itemColList)
        {
            if (!schemaSet.Contains(cid))
            {
                TempData["OrderError"] = "An item charge is not in the selected configuration schema.";
                return RedirectToOrderListFromModel(model);
            }
        }
        var qLevelList = SalesQuotationPricing.ParseIdList(model.QuotationLevelChargeIds);
        foreach (var qid in qLevelList)
        {
            if (!schemaSet.Contains(qid))
            {
                TempData["OrderError"] = "A document charge is not in the selected configuration schema.";
                return RedirectToOrderListFromModel(model);
            }
        }
        if (itemColList.Count > 0 && schemaSet.Count == 0)
        {
            TempData["OrderError"] = "Item charges require a configuration schema (with linked charges).";
            return RedirectToOrderListFromModel(model);
        }
        if (qLevelList.Count > 0 && schemaSet.Count == 0)
        {
            TempData["OrderError"] = "Document charges require a configuration schema (with linked charges).";
            return RedirectToOrderListFromModel(model);
        }
        IReadOnlyDictionary<int, Charge> chargeById = new Dictionary<int, Charge>();
        if (itemColList.Count > 0)
        {
            var needIds = itemColList.Distinct().ToList();
            var forLines = await _db.Charges.AsNoTracking()
                .Where(c => needIds.Contains(c.Id))
                .ToListAsync(ct)
                .ConfigureAwait(false);
            chargeById = forLines.ToDictionary(c => c.Id);
        }
        var itemColSet = new HashSet<int>(itemColList);
        var docValuesPosted = SalesQuotationPricing.ParseChargeValuesJson(model.QuotationChargeValuesJson);
        foreach (var kv in docValuesPosted.Keys)
        {
            if (!qLevelList.Contains(kv))
            {
                TempData["OrderError"] = "Document charge values do not match selected document charges.";
                return RedirectToOrderListFromModel(model);
            }
        }

        var lineEntities = new List<SalesOrderItem>();
        var rows = model.Items ?? new List<SalesQuotationItemFormRow>();
        foreach (var r in rows)
        {
            var mat = (r.MaterialNumber ?? "").Trim();
            if (string.IsNullOrEmpty(mat)) continue;

            var qty = r.OrderQuantity.GetValueOrDefault();
            if (DocumentQuantityRules.ValidateNonNegativeWhole(qty, $"Order quantity ({mat})") is { } qWholeErr)
            {
                TempData["OrderError"] = qWholeErr;
                return RedirectToOrderListFromModel(model);
            }
            if (qty <= 0) continue;
            if (r.QuantityUomId is not > 0) continue;
            var delDay = (r.DeliveryDate?.Date) ?? orderDay;
            if (strict)
            {
                if (r.UnitPrice is not > 0)
                {
                    TempData["OrderError"] = "Each line needs a valid unit price.";
                    return RedirectToOrderListFromModel(model);
                }
            }

            if (r.QuantityUomId is int uid && uid > 0)
            {
                var vUom = await MaterialUomForMaterialHelper.ValidateUomForMaterialAsync(_db, mat, uid, ct)
                    .ConfigureAwait(false);
                if (vUom != null)
                {
                    TempData["OrderError"] = vUom;
                    return RedirectToOrderListFromModel(model);
                }
            }

            var lineVals = SalesQuotationPricing.ParseChargeValuesJson(r.ItemChargeValuesJson);
            foreach (var kv in lineVals.Keys)
            {
                if (!itemColSet.Contains(kv))
                {
                    TempData["OrderError"] = "A line has item charge values for charges not selected as line columns.";
                    return RedirectToOrderListFromModel(model);
                }
            }

            var unitP = r.UnitPrice.GetValueOrDefault();
            if (unitP < 0)
            {
                TempData["OrderError"] = $"Unit price cannot be negative ({mat}).";
                return RedirectToOrderListFromModel(model);
            }
            const decimal discP = 0m;

            decimal sub;
            decimal lineTot;
            if (itemColList.Count == 0)
            {
                sub = Math.Round(unitP, 4, MidpointRounding.AwayFromZero);
                lineTot = sub;
            }
            else
            {
                (sub, lineTot) = SalesQuotationPricing.ComputeLineWithChargeValues(
                    itemColList, chargeById, lineVals, qty, unitP, discP);
            }
            if (strict && lineTot < 0)
            {
                TempData["OrderError"] = "Invalid line calculation.";
                return RedirectToOrderListFromModel(model);
            }

            var desc = (r.MaterialDescription ?? "").Trim();
            if (string.IsNullOrEmpty(desc))
            {
                desc = await _db.CreateMaterialMaster.AsNoTracking()
                    .Where(m => m.MaterialNumber == mat)
                    .Select(m => m.Description ?? "")
                    .FirstOrDefaultAsync(ct) ?? "";
            }

            string? storeLineJson = (r.ItemChargeValuesJson ?? "").Trim();
            if (itemColList.Count > 0 && string.IsNullOrEmpty(storeLineJson))
                storeLineJson = "{}";
            else if (string.IsNullOrEmpty(storeLineJson))
                storeLineJson = null;

            lineEntities.Add(new SalesOrderItem
            {
                MaterialNumber = mat,
                MaterialDescription = string.IsNullOrEmpty(desc) ? null : desc[..Math.Min(500, desc.Length)],
                QuantityUomId = r.QuantityUomId,
                SalesPriceGrade = NormalizeQuotationLineGrade(r.SalesPriceGrade),
                OrderQuantity = qty,
                UnitPrice = unitP,
                DiscountPercent = 0,
                SubtotalAfterDiscount = sub,
                TaxAmount = 0,
                NetPrice = lineTot,
                LineTaxChargeId = null,
                ItemAppliedChargeIds = null,
                ItemChargeValuesJson = storeLineJson,
                DeliveryDate = DateTime.SpecifyKind(delDay, DateTimeKind.Unspecified)
            });
        }

        if (strict)
        {
            if (lineEntities.Count == 0)
            {
                TempData["OrderError"] = "Confirm order requires at least one line with quantity.";
                return RedirectToOrderListFromModel(model);
            }
        }

        if (updateOrderId is int uoid)
        {
            var toUpdate = await _db.SalesOrders
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == uoid, ct)
                .ConfigureAwait(false);
            if (toUpdate == null)
            {
                TempData["OrderError"] = "Sales order not found.";
                return RedirectToOrderListFromModel(model);
            }
            if (toUpdate.Status != SalesOrder.StatusOpen)
            {
                TempData["OrderError"] = "Only open orders can be updated.";
                return RedirectToOrderListFromModel(model);
            }
            _db.SalesOrderItems.RemoveRange(toUpdate.Items);
            toUpdate.SalesQuotationId = model.SalesQuotationId is > 0 ? model.SalesQuotationId : null;
            toUpdate.PlantId = string.IsNullOrWhiteSpace(model.PlantId) ? null : model.PlantId;
            toUpdate.DistributionChannelId = model.DistributionChannelId;
            toUpdate.ConfigurationSchemaId = model.ConfigurationSchemaId is int schemaId && schemaId > 0 ? schemaId : null;
            toUpdate.CustomerBusinessPartnerId = string.IsNullOrWhiteSpace(model.CustomerBusinessPartnerId) ? null : model.CustomerBusinessPartnerId;
            toUpdate.CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName;
            toUpdate.ShipToAddress = string.IsNullOrWhiteSpace(shipTo) ? null : shipTo;
            toUpdate.SalesPersonId = string.IsNullOrWhiteSpace(model.SalesPersonId) ? null : model.SalesPersonId;
            toUpdate.PriceListCode = string.IsNullOrWhiteSpace(model.PriceListCode) ? null : model.PriceListCode;
            toUpdate.PaymentTerm = string.IsNullOrWhiteSpace(model.PaymentTerm) ? null : model.PaymentTerm!.Trim();
            toUpdate.Remarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks!.Trim();
            toUpdate.QuotationLevelChargeIds = string.IsNullOrWhiteSpace(model.QuotationLevelChargeIds) ? null : model.QuotationLevelChargeIds!.Trim();
            toUpdate.ItemChargeColumnIds = string.IsNullOrWhiteSpace(model.ItemChargeColumnIds) ? null : model.ItemChargeColumnIds!.Trim();
            toUpdate.QuotationChargeValuesJson = string.IsNullOrWhiteSpace(model.QuotationChargeValuesJson) ? null : model.QuotationChargeValuesJson!.Trim();
            toUpdate.OrderDate = orderDay;
            toUpdate.RequestedDeliveryDate = reqDel;
            toUpdate.Status = isDraftSubmit
                ? SalesOrder.StatusOpen
                : (isConfirmSubmit ? SalesOrder.StatusConfirmed : SalesOrder.StatusOpen);
            foreach (var li in lineEntities) toUpdate.Items.Add(li);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            if (isConfirmSubmit && string.Equals(toUpdate.Status, SalesOrder.StatusConfirmed, StringComparison.OrdinalIgnoreCase))
            {
                var (giOk, giMsg, _) = await _salesGiService.CreateOrOpenPendingAsync(
                    toUpdate.Id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    ct).ConfigureAwait(false);
                if (!giOk)
                    TempData["OrderWarning"] = giMsg;
            }
            TempData["OrderMessage"] = isDraftSubmit
                ? "Sales order updated (open)."
                : "Sales order confirmed.";
            return RedirectToOrderListFromModel(model);
        }

        string number;
        try
        {
            number = await _documentNumbers.AllocateAsync(ModuleKeys.SaleOrder, ct).ConfigureAwait(false);
        }
        catch (DocumentIntegrationMissingException ex)
        {
            TempData["OrderError"] = ex.Message;
            return RedirectToOrderListFromModel(model);
        }
        catch (DocumentIntegrationRangeExhaustedException ex)
        {
            TempData["OrderError"] = ex.Message;
            return RedirectToOrderListFromModel(model);
        }

        var header = new SalesOrder
        {
            SalesQuotationId = model.SalesQuotationId is > 0 ? model.SalesQuotationId : null,
            SalesOrderNumber = number,
            PlantId = string.IsNullOrWhiteSpace(model.PlantId) ? null : model.PlantId,
            DistributionChannelId = model.DistributionChannelId,
            ConfigurationSchemaId = model.ConfigurationSchemaId is int cs && cs > 0 ? cs : null,
            CustomerBusinessPartnerId = string.IsNullOrWhiteSpace(model.CustomerBusinessPartnerId) ? null : model.CustomerBusinessPartnerId,
            CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName,
            ShipToAddress = string.IsNullOrWhiteSpace(shipTo) ? null : shipTo,
            SalesPersonId = string.IsNullOrWhiteSpace(model.SalesPersonId) ? null : model.SalesPersonId,
            PriceListCode = string.IsNullOrWhiteSpace(model.PriceListCode) ? null : model.PriceListCode,
            PaymentTerm = string.IsNullOrWhiteSpace(model.PaymentTerm) ? null : model.PaymentTerm!.Trim(),
            Remarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks!.Trim(),
            QuotationLevelChargeIds = string.IsNullOrWhiteSpace(model.QuotationLevelChargeIds) ? null : model.QuotationLevelChargeIds!.Trim(),
            ItemChargeColumnIds = string.IsNullOrWhiteSpace(model.ItemChargeColumnIds) ? null : model.ItemChargeColumnIds!.Trim(),
            QuotationChargeValuesJson = string.IsNullOrWhiteSpace(model.QuotationChargeValuesJson) ? null : model.QuotationChargeValuesJson!.Trim(),
            OrderDate = orderDay,
            RequestedDeliveryDate = reqDel,
            Status = isDraftSubmit
                ? SalesOrder.StatusOpen
                : (isConfirmSubmit ? SalesOrder.StatusConfirmed : SalesOrder.StatusOpen)
        };
        foreach (var li in lineEntities) header.Items.Add(li);

        _db.SalesOrders.Add(header);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        if (isConfirmSubmit && string.Equals(header.Status, SalesOrder.StatusConfirmed, StringComparison.OrdinalIgnoreCase))
        {
            var (giOk, giMsg, _) = await _salesGiService.CreateOrOpenPendingAsync(
                header.Id,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                ct).ConfigureAwait(false);
            if (!giOk)
                TempData["OrderWarning"] = giMsg;
        }
        TempData["OrderMessage"] = isDraftSubmit
            ? "Sales order saved (open)."
            : "Sales order confirmed.";
        return RedirectToOrderListFromModel(model);
    }

    [HttpGet]
    public async Task<JsonResult> OrderData(int? id, CancellationToken ct = default)
    {
        if (id is not > 0)
            return Json(new { success = false, message = "Invalid id." });
        var o = await _db.SalesOrders.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);
        if (o == null)
            return Json(new { success = false, message = "Sales order not found." });
        if (o.Status != SalesOrder.StatusOpen)
            return Json(new { success = false, message = "Only open orders can be opened for editing." });

        object? schemaCharges = null;
        if (o.ConfigurationSchemaId is int sch && sch > 0)
        {
            schemaCharges = await _db.ConfigurationSchemaCharges.AsNoTracking()
                .Where(x => x.ConfigurationSchemaId == sch)
                .Join(_db.Charges.AsNoTracking(), c => c.ChargeId, ch => ch.Id, (_, ch) => ch)
                .OrderBy(ch => ch.Symbol)
                .Select(ch => new
                {
                    id = ch.Id,
                    symbol = ch.Symbol,
                    description = ch.Description,
                    valueType = ch.ValueType,
                    sign = ch.Sign,
                    defaultPercent = ch.DefaultPercent
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        return Json(new
        {
            success = true,
            customerCatalogEntry = string.IsNullOrWhiteSpace(o.CustomerBusinessPartnerId)
                ? null
                : new
                {
                    bpId = o.CustomerBusinessPartnerId,
                    displayName = o.CustomerName ?? o.CustomerBusinessPartnerId,
                    shipTo = o.ShipToAddress ?? ""
                },
            h = new
            {
                id = o.Id,
                salesOrderNumber = o.SalesOrderNumber,
                salesQuotationId = o.SalesQuotationId,
                plantId = o.PlantId,
                distributionChannelId = o.DistributionChannelId,
                configurationSchemaId = o.ConfigurationSchemaId,
                customerBusinessPartnerId = o.CustomerBusinessPartnerId,
                customerName = o.CustomerName,
                shipToAddress = o.ShipToAddress,
                itemChargeColumnIds = o.ItemChargeColumnIds,
                quotationLevelChargeIds = o.QuotationLevelChargeIds,
                quotationChargeValuesJson = o.QuotationChargeValuesJson,
                orderDate = o.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                requestedDeliveryDate = o.RequestedDeliveryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            },
            items = o.Items.OrderBy(x => x.Id).Select(i => new
            {
                i.MaterialNumber,
                i.MaterialDescription,
                i.QuantityUomId,
                i.SalesPriceGrade,
                orderQuantity = i.OrderQuantity,
                i.UnitPrice,
                i.DiscountPercent,
                i.ItemChargeValuesJson,
                deliveryDate = i.DeliveryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? ""
            }).ToList(),
            schemaCharges
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, SalesOrderCreateFormModel model, CancellationToken ct = default) =>
        await Create(model, ct, updateOrderId: id).ConfigureAwait(false);

    [HttpGet]
    public async Task<IActionResult> CreateFromQuotation(
        int quotationId,
        string? returnQ,
        string? returnStatus,
        string? returnPlantId,
        int? returnDistributionChannelId,
        CancellationToken ct = default)
    {
        var q = await _db.SalesQuotations
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == quotationId, ct)
            .ConfigureAwait(false);
        if (q == null)
        {
            TempData["QuotationError"] = "Quotation not found.";
            return RedirectToQuotationList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
        }
        if (q.Status != SalesQuotation.StatusSent)
        {
            TempData["QuotationError"] = "Only sent quotations can be converted to a sales order.";
            return RedirectToQuotationList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
        }

        var existing = await _db.SalesOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.SalesQuotationId == quotationId, ct)
            .ConfigureAwait(false);
        if (existing != null)
        {
            if (string.Equals(existing.Status, SalesOrder.StatusOpen, StringComparison.OrdinalIgnoreCase))
            {
                TempData["QuotationMessage"] = "A sales order already exists for this quotation. Open Sales Order to edit it.";
                return RedirectToAction(nameof(Index), "SalesOrder", new { editId = existing.Id });
            }

            TempData["QuotationError"] = "This quotation already has a confirmed sales order; a new sales order cannot be created from it.";
            return RedirectToQuotationList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
        }

        string number;
        try
        {
            number = await _documentNumbers.AllocateAsync(ModuleKeys.SaleOrder, ct).ConfigureAwait(false);
        }
        catch (DocumentIntegrationMissingException ex)
        {
            TempData["QuotationError"] = ex.Message;
            return RedirectToQuotationList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
        }
        catch (DocumentIntegrationRangeExhaustedException ex)
        {
            TempData["QuotationError"] = ex.Message;
            return RedirectToQuotationList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
        }

        var now = DateTime.Today;
        var header = new SalesOrder
        {
            SalesQuotationId = q.Id,
            SalesOrderNumber = number,
            PlantId = q.PlantId,
            DistributionChannelId = q.DistributionChannelId,
            ConfigurationSchemaId = q.ConfigurationSchemaId,
            CustomerBusinessPartnerId = q.CustomerBusinessPartnerId,
            CustomerName = q.CustomerName,
            ShipToAddress = q.ShipToAddress,
            SalesPersonId = q.SalesPersonId,
            PriceListCode = q.PriceListCode,
            PaymentTerm = q.PaymentTerm,
            Remarks = q.Remarks,
            QuotationLevelChargeIds = q.QuotationLevelChargeIds,
            ItemChargeColumnIds = q.ItemChargeColumnIds,
            QuotationChargeValuesJson = q.QuotationChargeValuesJson,
            OrderDate = now,
            RequestedDeliveryDate = q.ValidityDate >= now ? q.ValidityDate : now,
            Status = SalesOrder.StatusOpen
        };
        var itemColList = SalesQuotationPricing.ParseIdList(q.ItemChargeColumnIds);
        IReadOnlyDictionary<int, Charge> chargeById = new Dictionary<int, Charge>();
        if (itemColList.Count > 0)
        {
            var needIds = itemColList.Distinct().ToList();
            var forLines = await _db.Charges.AsNoTracking()
                .Where(c => needIds.Contains(c.Id))
                .ToListAsync(ct)
                .ConfigureAwait(false);
            chargeById = forLines.ToDictionary(c => c.Id);
        }
        foreach (var li in q.Items)
        {
            var qty = li.OrderQuantity;
            var unitP = li.UnitPrice;
            var lineVals = SalesQuotationPricing.ParseChargeValuesJson(li.ItemChargeValuesJson);
            decimal sub;
            decimal lineTot;
            if (itemColList.Count == 0)
            {
                sub = Math.Round(unitP, 4, MidpointRounding.AwayFromZero);
                lineTot = sub;
            }
            else
            {
                (sub, lineTot) = SalesQuotationPricing.ComputeLineWithChargeValues(
                    itemColList, chargeById, lineVals, qty, unitP, 0m);
            }
            header.Items.Add(new SalesOrderItem
            {
                MaterialNumber = li.MaterialNumber,
                MaterialDescription = li.MaterialDescription,
                SalesPriceGrade = li.SalesPriceGrade,
                QuantityUomId = li.QuantityUomId,
                OrderQuantity = li.OrderQuantity,
                UnitPrice = li.UnitPrice,
                DiscountPercent = 0,
                SubtotalAfterDiscount = sub,
                TaxAmount = li.TaxAmount,
                NetPrice = Math.Round(li.UnitPrice, 4, MidpointRounding.AwayFromZero),
                LineTaxChargeId = li.LineTaxChargeId,
                ItemAppliedChargeIds = li.ItemAppliedChargeIds,
                ItemChargeValuesJson = li.ItemChargeValuesJson,
                DeliveryDate = li.DeliveryDate
            });
        }
        _db.SalesOrders.Add(header);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        TempData["OrderMessage"] = $"Sales order {header.SalesOrderNumber} was created from quotation {q.QuotationNumber}.";
        return RedirectToAction(nameof(Index), new { editId = header.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int id,
        [FromForm] string? returnQ,
        [FromForm] string? returnStatus,
        [FromForm] string? returnPlantId,
        [FromForm] int? returnDistributionChannelId,
        CancellationToken ct = default)
    {
        var o = await _db.SalesOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);
        if (o == null)
        {
            TempData["OrderError"] = "Sales order not found.";
            return RedirectToOrderList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
        }
        if (!string.Equals(o.Status, SalesOrder.StatusOpen, StringComparison.OrdinalIgnoreCase))
        {
            TempData["OrderError"] = "Only open sales orders can be deleted.";
            return RedirectToOrderList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
        }
        _db.SalesOrderItems.RemoveRange(o.Items);
        _db.SalesOrders.Remove(o);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        TempData["OrderMessage"] = "Sales order deleted.";
        return RedirectToOrderList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrOpenGoodsIssue(
        int id,
        [FromForm] string? returnQ,
        [FromForm] string? returnStatus,
        [FromForm] string? returnPlantId,
        [FromForm] int? returnDistributionChannelId,
        CancellationToken ct = default)
    {
        var (ok, message, _) = await _salesGiService.CreateOrOpenPendingAsync(
            id,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            ct);
        if (ok) TempData["OrderMessage"] = message;
        else TempData["OrderError"] = message;
        return RedirectToOrderList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoodsReceive(
        int id,
        [FromForm] string? returnQ,
        [FromForm] string? returnStatus,
        [FromForm] string? returnPlantId,
        [FromForm] int? returnDistributionChannelId,
        CancellationToken ct = default)
    {
        var (ok, message, _) = await _salesGiService.ReceiveBySalesOrderAsync(
            id,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            ct);
        if (ok) TempData["OrderMessage"] = message;
        else TempData["OrderError"] = message;
        return RedirectToOrderList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
    }

    private async Task<SalesOrder?> LoadSalesOrderForPdfAsync(int id, CancellationToken ct) =>
        await _db.SalesOrders.AsNoTracking()
            .Include(x => x.Items)!.ThenInclude(i => i.QuantityUom)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

    private static string SafeSalesOrderPdfName(string number)
    {
        var s = (number ?? "order").Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '-');
        if (string.IsNullOrEmpty(s)) s = "order";
        if (!s.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) s += ".pdf";
        return s;
    }

    private IActionResult RedirectToOrderList(
        string? returnQ, string? returnStatus, string? returnPlantId, int? returnDistributionChannelId) =>
        RedirectToAction(nameof(Index), new
        {
            q = string.IsNullOrWhiteSpace(returnQ) ? null : returnQ,
            status = string.IsNullOrWhiteSpace(returnStatus) ? null : returnStatus,
            plantId = string.IsNullOrWhiteSpace(returnPlantId) ? null : returnPlantId,
            distributionChannelId = returnDistributionChannelId
        });

    private IActionResult RedirectToQuotationList(
        string? returnQ, string? returnStatus, string? returnPlantId, int? returnDistributionChannelId) =>
        RedirectToAction("Index", "SalesQuotation", new
        {
            q = string.IsNullOrWhiteSpace(returnQ) ? null : returnQ,
            status = string.IsNullOrWhiteSpace(returnStatus) ? null : returnStatus,
            plantId = string.IsNullOrWhiteSpace(returnPlantId) ? null : returnPlantId,
            distributionChannelId = returnDistributionChannelId
        });

    private IActionResult RedirectToOrderListFromModel(SalesOrderCreateFormModel? m) =>
        RedirectToOrderList(m?.ReturnQ, m?.ReturnStatus, m?.ReturnPlantId, m?.ReturnDistributionChannelId);

    private static string NormalizeQuotationLineGrade(string? g)
    {
        var s = (g ?? "").Trim();
        if (string.IsNullOrEmpty(s) || s.Equals(StockInventoryGradeCodes.FirstQuality, StringComparison.OrdinalIgnoreCase))
            return StockInventoryGradeCodes.FirstQuality;
        if (s.Equals(StockInventoryGradeCodes.SecondQuality, StringComparison.OrdinalIgnoreCase) || s.Equals("B", StringComparison.OrdinalIgnoreCase))
            return StockInventoryGradeCodes.SecondQuality;
        if (s.Equals(StockInventoryGradeCodes.ThirdQuality, StringComparison.OrdinalIgnoreCase) || s.Equals("C", StringComparison.OrdinalIgnoreCase))
            return StockInventoryGradeCodes.ThirdQuality;
        if (s.Equals(StockInventoryGradeCodes.Scrap, StringComparison.OrdinalIgnoreCase))
            return StockInventoryGradeCodes.FirstQuality;
        return StockInventoryGradeCodes.FirstQuality;
    }

    private static string BuildBpShipTo(BusinessPartnerMasterSample bp)
    {
        var parts = new[] { bp.HouseNo, bp.Street, bp.City, bp.PostalCode, bp.Country }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(", ", parts);
    }

    private async Task ApplyCustomerDrivenDefaultsAsync(SalesOrderCreateFormModel model, BusinessPartnerMasterSample bp, CancellationToken ct)
    {
        model.DistributionChannelId = await ResolveDistributionChannelIdAsync(bp.DistChannel, ct).ConfigureAwait(false);
        model.ConfigurationSchemaId = await ResolveSalesSchemaIdAsync(bp.SalesSchema, ct).ConfigureAwait(false);
        model.PlantId = await ResolvePlantIdFromSalesSchemaAsync(bp.SalesSchema, ct).ConfigureAwait(false);
    }

    private async Task<int?> ResolveDistributionChannelIdAsync(string? customerDistChannel, CancellationToken ct)
    {
        var raw = (customerDistChannel ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        static string Norm(string? s) => (s ?? "").Replace(" ", "").Trim().ToLowerInvariant();

        // Small lookup table; keep normalization logic out of EF queries.
        var chans = await _db.DistributionChannels.AsNoTracking()
            .Select(x => new { x.DistributionChannelID, x.DistributionChannelName })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (int.TryParse(raw, out var byId) && byId > 0)
        {
            if (chans.Any(x => x.DistributionChannelID == byId))
                return byId;

            // Backward compat: older BP forms stored "1"/"2" as fake IDs for these two options.
            // If those IDs don't exist in the DB, map them to the real DB rows by name.
            var legacyName = byId == 1 ? "On Call" : (byId == 2 ? "Direct Sales" : null);
            if (legacyName != null)
            {
                var want = Norm(legacyName);
                var match = chans.FirstOrDefault(x => Norm(x.DistributionChannelName) == want);
                return match?.DistributionChannelID;
            }
            return null;
        }

        {
            var want = Norm(raw);
            var match = chans.FirstOrDefault(x => Norm(x.DistributionChannelName) == want);
            return match?.DistributionChannelID;
        }
    }

    private async Task<int?> ResolveSalesSchemaIdAsync(string? bpSalesSchema, CancellationToken ct)
    {
        var raw = (bpSalesSchema ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (int.TryParse(raw, out var byId) && byId > 0)
            return await _db.ConfigurationSchemas.AsNoTracking()
                .AnyAsync(x => x.Id == byId && x.SchemaType == ConfigurationSchemaType.Sales, ct).ConfigureAwait(false) ? byId : null;
        return await _db.ConfigurationSchemas.AsNoTracking()
            .Where(x => x.SchemaType == ConfigurationSchemaType.Sales && x.Title == raw)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<string?> ResolvePlantIdFromSalesSchemaAsync(string? bpSalesSchema, CancellationToken ct)
    {
        var raw = (bpSalesSchema ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // BP can store schema as ID (preferred) or as title; handle both.
        var schemaTitle = raw;
        if (int.TryParse(raw, out var schemaId) && schemaId > 0)
        {
            schemaTitle = await _db.ConfigurationSchemas.AsNoTracking()
                .Where(s => s.Id == schemaId && s.SchemaType == ConfigurationSchemaType.Sales)
                .Select(s => s.Title)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false) ?? raw;
        }

        var plantName = schemaTitle.Equals("WalkIn", StringComparison.OrdinalIgnoreCase)
            ? "Emporium"
            : (schemaTitle.Equals("Dealer", StringComparison.OrdinalIgnoreCase) ? "Manufacturing Plant" : null);
        if (plantName == null)
            return null;
        return await _db.PlantsSamples.AsNoTracking()
            .Where(p => p.PlantName == plantName)
            .Select(p => p.PlantID)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>Validates customer schema for Emporium (WalkIn) vs other plants (Dealer).</summary>
    private async Task<string?> ValidateCustomerSalesSchemaAsync(
        BusinessPartnerMasterSample bp,
        string? plantId,
        bool forSalesOrder,
        CancellationToken ct)
    {
        var walkInSchemaId = await SalesSchemaResolution.GetWalkInSchemaIdAsync(_db, ct).ConfigureAwait(false);
        var dealerSchemaId = await SalesSchemaResolution.GetDealerSchemaIdAsync(_db, ct).ConfigureAwait(false);
        var isEmporiumUser = UserPlantResolution.HasEmporiumStorePlant(User) && walkInSchemaId is > 0;
        var plant = (plantId ?? string.Empty).Trim();
        var isEmporiumPlant = string.Equals(plant, UserPlantResolution.EmporiumPlantId, StringComparison.OrdinalIgnoreCase);
        var requireWalkIn = isEmporiumPlant || (plant.Length == 0 && isEmporiumUser);

        var docLabel = forSalesOrder ? "sales orders" : "sales quotations";
        if (requireWalkIn)
        {
            if (walkInSchemaId is not > 0)
                return null;
            if (!SalesSchemaResolution.MatchesWalkInSchema(bp.SalesSchema, walkInSchemaId.Value))
                return $"Only WalkIn customers can be used for Emporium {docLabel}.";
            return null;
        }

        if (dealerSchemaId is not > 0)
            return null;
        if (!SalesSchemaResolution.MatchesDealerSchema(bp.SalesSchema, dealerSchemaId.Value))
            return $"Only Dealer customers can be used for {docLabel} outside Emporium plant.";
        return null;
    }
}
