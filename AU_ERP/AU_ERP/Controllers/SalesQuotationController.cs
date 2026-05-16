using System.Globalization;
using System.Net;
using System.Net.Mail;
using AU_ERP.Configuration;
using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using AU_ERP.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "SalesDepartment")]
public class SalesQuotationController : Controller
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly DocumentNumberAllocator _documentNumbers;
    private readonly CompanyInfoService _companyInfo;
    private readonly EmporiumWalkInCustomerService _emporiumWalkIn;

    public SalesQuotationController(
        AppDbContext db,
        IEmailService emailService,
        IConfiguration configuration,
        DocumentNumberAllocator documentNumbers,
        CompanyInfoService companyInfo,
        EmporiumWalkInCustomerService emporiumWalkIn)
    {
        _db = db;
        _emailService = emailService;
        _configuration = configuration;
        _documentNumbers = documentNumbers;
        _companyInfo = companyInfo;
        _emporiumWalkIn = emporiumWalkIn;
    }

    /// <summary>FERT materials for quotation line picker (same JSON shape as BOM material search).</summary>
    [HttpGet]
    public async Task<JsonResult> SearchFertMaterialsForQuotation(string? q, CancellationToken ct = default)
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
    public async Task<JsonResult> NextQuotationNumber(CancellationToken ct = default)
    {
        try
        {
            var n = await _documentNumbers.PeekNextAsync(ModuleKeys.SaleQuotation, ct).ConfigureAwait(false);
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
    public async Task<JsonResult> MaterialUomContextForQuotation(string? materialNumber, int? includeUomIdForEdit, CancellationToken ct = default)
    {
        var (success, errorMessage, data) =
            await MaterialUomForMaterialHelper.TryBuildMaterialUomContextAsync(_db, materialNumber, includeUomIdForEdit, ct);
        if (!success)
            return Json(new { success = false, message = errorMessage });
        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<JsonResult> QuotationLineUnitCost(string? materialNumber, int? uomId, string? priceGrade, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(materialNumber) || uomId is not > 0)
            return Json(new { success = false, message = "Material and UOM are required." });
        var g = (priceGrade ?? "").Trim();
        if (string.IsNullOrEmpty(g)) g = StockInventoryGradeCodes.FirstQuality;
        var key = materialNumber.Trim();

        var mat = await _db.CreateMaterialMaster.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MaterialNumber == key, ct)
            .ConfigureAwait(false);
        if (mat == null)
            return Json(new { success = false, message = "Material not found." });

        var basePrice = InventoryStandardCostService.GetMaterialPricePerBaseUom(mat, g);
        if (basePrice is not decimal p || p <= 0)
            return Json(new { success = true, stdCostPerUom = 0m });

        var toBase = await UnitConversionMath.ToBaseAsync(_db, key, 1m, uomId.Value, ct)
            .ConfigureAwait(false);
        if (!toBase.ok)
            return Json(new { success = false, message = toBase.error ?? "UOM conversion not configured for this material." });

        var unitPrice = Math.Round(toBase.quantityBase * p, 4, MidpointRounding.AwayFromZero);
        return Json(new { success = true, stdCostPerUom = unitPrice });
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
        IQueryable<SalesQuotation> query = _db.SalesQuotations.AsNoTracking()
            .Include(sq => sq.Plant)
            .Include(sq => sq.DistributionChannel);

        var qq = (q ?? "").Trim();
        if (qq.Length > 0)
        {
            query = query.Where(x =>
                x.QuotationNumber.Contains(qq)
                || (x.CustomerName != null && x.CustomerName.Contains(qq)));
        }

        var st = (status ?? "All").Trim();
        if (string.Equals(st, SalesQuotation.StatusDraft, StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Status == SalesQuotation.StatusDraft);
        else if (string.Equals(st, SalesQuotation.StatusSent, StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Status == SalesQuotation.StatusSent);

        if (!string.IsNullOrWhiteSpace(plantId))
            query = query.Where(x => x.PlantId == plantId);
        if (distributionChannelId is { } dcid && dcid > 0)
            query = query.Where(x => x.DistributionChannelId == dcid);

        var list = await query
            .OrderByDescending(sq => sq.QuotationDate)
            .ThenBy(sq => sq.QuotationNumber)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var linkedStatusByQid = new Dictionary<int, string>();
        if (list.Count > 0)
        {
            var qIds = list.Select(sq => sq.Id).ToList();
            var orderRows = await _db.SalesOrders.AsNoTracking()
                .Where(o => o.SalesQuotationId != null && qIds.Contains(o.SalesQuotationId.Value))
                .Select(o => new { o.SalesQuotationId, o.Id, o.Status })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            foreach (var g in orderRows.GroupBy(r => r.SalesQuotationId!.Value))
            {
                var pick = g.OrderBy(r => r.Id).First();
                linkedStatusByQid[g.Key] = pick.Status;
            }
        }

        var customers = await _db.BusinessPartnerMasterSamples.AsNoTracking()
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
        var walkInSchemaId = await _emporiumWalkIn.GetWalkInSalesSchemaIdAsync(ct).ConfigureAwait(false);
        ViewBag.WalkInSchemaId = walkInSchemaId ?? 0;
        ViewBag.IsEmporiumWalkInUser = UserPlantResolution.HasEmporiumStorePlant(User) && walkInSchemaId is > 0;
        var vm = new SalesQuotationListVm
        {
            Items = list,
            LinkedSalesOrderStatusByQuotationId = linkedStatusByQid,
            Q = string.IsNullOrEmpty(qq) ? null : qq,
            Status = string.IsNullOrEmpty(st) ? "All" : st,
            PlantId = string.IsNullOrWhiteSpace(plantId) ? null : plantId,
            DistributionChannelId = distributionChannelId is > 0 ? distributionChannelId : null
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesQuotationCreateFormModel model, CancellationToken ct = default, int? updateQuotationId = null)
    {
        var isDraftSubmit = string.Equals(model.SubmitAction, "draft", StringComparison.OrdinalIgnoreCase);
        var isSendSubmit = string.Equals(model.SubmitAction, "send", StringComparison.OrdinalIgnoreCase);
        var isEmailSubmit = string.Equals(model.SubmitAction, "email", StringComparison.OrdinalIgnoreCase);
        var strict = isSendSubmit || isEmailSubmit;

        if (strict)
        {
            if (!model.QuotationDate.HasValue || !model.ValidityDate.HasValue)
            {
                TempData["QuotationError"] = "This action requires quotation and validity dates.";
                return RedirectToQuotationListFromModel(model);
            }
        }

        if (isEmailSubmit)
        {
            if (string.IsNullOrWhiteSpace(_configuration["Email:SmtpHost"]))
            {
                TempData["QuotationError"] =
                    "Email is not configured. Set Email:SmtpHost and related settings in appsettings (see Email section).";
                return RedirectToQuotationListFromModel(model);
            }
            if (string.IsNullOrWhiteSpace(model.CustomerBusinessPartnerId))
            {
                TempData["QuotationError"] = "Email to customer requires a sold-to customer.";
                return RedirectToQuotationListFromModel(model);
            }
            var bpForEmail = await _db.BusinessPartnerMasterSamples.AsNoTracking()
                .FirstOrDefaultAsync(c => c.BPID == model.CustomerBusinessPartnerId, ct)
                .ConfigureAwait(false);
            var em0 = (bpForEmail?.Email ?? "").Trim();
            if (string.IsNullOrWhiteSpace(em0))
            {
                TempData["QuotationError"] =
                    "The customer has no email address. Add it on the business partner (Communication tab) before sending.";
                return RedirectToQuotationListFromModel(model);
            }
            try
            {
                _ = new MailAddress(em0);
            }
            catch
            {
                TempData["QuotationError"] =
                    "The customer's email address is not valid. Update it on the business partner (Communication).";
                return RedirectToQuotationListFromModel(model);
            }
        }

        var now = DateTime.Today;
        var qd = (model.QuotationDate?.Date) ?? now;
        var vd = (model.ValidityDate?.Date) ?? now;
        if (strict && vd < qd)
        {
            TempData["QuotationError"] = "Validity date must be on or after quotation date.";
            return RedirectToQuotationListFromModel(model);
        }

        string? customerName = null;
        string? customerDistChannel = null;
        string? customerSalesSchema = null;
        string? customerShipTo = null;
        if (!string.IsNullOrWhiteSpace(model.CustomerBusinessPartnerId))
        {
            var bp = await _db.BusinessPartnerMasterSamples.AsNoTracking()
                .FirstOrDefaultAsync(c => c.BPID == model.CustomerBusinessPartnerId, ct)
                .ConfigureAwait(false);
            if (bp == null)
            {
                TempData["QuotationError"] = "Invalid customer.";
                return RedirectToQuotationListFromModel(model);
            }
            customerName = (bp.FullName ?? "").Trim();
            customerDistChannel = bp.DistChannel;
            customerSalesSchema = bp.SalesSchema;
            customerShipTo = BuildBpShipTo(bp);
            await ApplyCustomerDrivenDefaultsAsync(model, bp, ct).ConfigureAwait(false);
        }
        else if (strict)
        {
            TempData["QuotationError"] = "Select a customer.";
            return RedirectToQuotationListFromModel(model);
        }

        var shipTo = (model.ShipToAddress ?? "").Trim();
        if (string.IsNullOrWhiteSpace(shipTo) && !string.IsNullOrWhiteSpace(customerShipTo))
            shipTo = customerShipTo;
        if (strict && string.IsNullOrWhiteSpace(shipTo))
        {
            TempData["QuotationError"] = "Send to customer requires a ship-to address (enter or fill from customer).";
            return RedirectToQuotationListFromModel(model);
        }
        if (strict && (string.IsNullOrWhiteSpace(model.PlantId)
            || !model.DistributionChannelId.HasValue
            || !model.ConfigurationSchemaId.HasValue
            || model.ConfigurationSchemaId is not > 0))
        {
            TempData["QuotationError"] = "This action requires plant, distribution channel, and configuration schema.";
            return RedirectToQuotationListFromModel(model);
        }
        if (strict && model.ConfigurationSchemaId is int strictSchemaId)
        {
            var schemaOk = await _db.ConfigurationSchemas.AsNoTracking()
                .AnyAsync(s => s.Id == strictSchemaId && s.SchemaType == ConfigurationSchemaType.Sales, ct)
                .ConfigureAwait(false);
            if (!schemaOk)
            {
                TempData["QuotationError"] = "Invalid configuration schema.";
                return RedirectToQuotationListFromModel(model);
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
                TempData["QuotationError"] = "An item charge is not in the selected configuration schema.";
                return RedirectToQuotationListFromModel(model);
            }
        }
        var qLevelList = SalesQuotationPricing.ParseIdList(model.QuotationLevelChargeIds);
        foreach (var qid in qLevelList)
        {
            if (!schemaSet.Contains(qid))
            {
                TempData["QuotationError"] = "A quotation charge is not in the selected configuration schema.";
                return RedirectToQuotationListFromModel(model);
            }
        }
        if (itemColList.Count > 0 && schemaSet.Count == 0)
        {
            TempData["QuotationError"] = "Item charges require a configuration schema (with linked charges).";
            return RedirectToQuotationListFromModel(model);
        }
        if (qLevelList.Count > 0 && schemaSet.Count == 0)
        {
            TempData["QuotationError"] = "Quotation charges require a configuration schema (with linked charges).";
            return RedirectToQuotationListFromModel(model);
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
                TempData["QuotationError"] = "Quotation charge values do not match selected quotation charges.";
                return RedirectToQuotationListFromModel(model);
            }
        }

        var lineEntities = new List<SalesQuotationItem>();
        var rows = model.Items ?? new List<SalesQuotationItemFormRow>();
        foreach (var r in rows)
        {
            var mat = (r.MaterialNumber ?? "").Trim();
            if (string.IsNullOrEmpty(mat)) continue;

            var qty = r.OrderQuantity.GetValueOrDefault();
            if (DocumentQuantityRules.ValidateNonNegativeWhole(qty, $"Order quantity ({mat})") is { } qWholeErrSq)
            {
                TempData["QuotationError"] = qWholeErrSq;
                return RedirectToQuotationListFromModel(model);
            }
            if (qty <= 0) continue;
            if (r.QuantityUomId is not > 0) continue;
            var delDay = (r.DeliveryDate?.Date) ?? qd;
            if (strict)
            {
                if (r.UnitPrice is not > 0)
                {
                    TempData["QuotationError"] = "Each line needs a valid unit price.";
                    return RedirectToQuotationListFromModel(model);
                }
            }

            if (r.QuantityUomId is int uid && uid > 0)
            {
                var vUom = await MaterialUomForMaterialHelper.ValidateUomForMaterialAsync(_db, mat, uid, ct)
                    .ConfigureAwait(false);
                if (vUom != null)
                {
                    TempData["QuotationError"] = vUom;
                    return RedirectToQuotationListFromModel(model);
                }
            }

            var lineVals = SalesQuotationPricing.ParseChargeValuesJson(r.ItemChargeValuesJson);
            foreach (var kv in lineVals.Keys)
            {
                if (!itemColSet.Contains(kv))
                {
                    TempData["QuotationError"] = "A line has item charge values for charges not selected as line columns.";
                    return RedirectToQuotationListFromModel(model);
                }
            }

            var unitP = r.UnitPrice.GetValueOrDefault();
            if (unitP < 0)
            {
                TempData["QuotationError"] = $"Unit price cannot be negative ({mat}).";
                return RedirectToQuotationListFromModel(model);
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
                TempData["QuotationError"] = "Invalid line calculation.";
                return RedirectToQuotationListFromModel(model);
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

            lineEntities.Add(new SalesQuotationItem
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
                TempData["QuotationError"] = "Send to customer requires at least one line with quantity and delivery date.";
                return RedirectToQuotationListFromModel(model);
            }
        }

        if (updateQuotationId is int uqid)
        {
            var toUpdate = await _db.SalesQuotations
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == uqid, ct)
                .ConfigureAwait(false);
            if (toUpdate == null)
            {
                TempData["QuotationError"] = "Quotation not found.";
                return RedirectToQuotationListFromModel(model);
            }
            if (toUpdate.Status != SalesQuotation.StatusDraft)
            {
                TempData["QuotationError"] = "Only draft quotations can be updated.";
                return RedirectToQuotationListFromModel(model);
            }
            _db.SalesQuotationItems.RemoveRange(toUpdate.Items);
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
            toUpdate.QuotationDate = qd;
            toUpdate.ValidityDate = vd;
            toUpdate.Status = isDraftSubmit
                ? SalesQuotation.StatusDraft
                : (isSendSubmit ? SalesQuotation.StatusSent : SalesQuotation.StatusDraft);
            foreach (var li in lineEntities) toUpdate.Items.Add(li);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            if (isEmailSubmit)
            {
                var (ok, err) = await TrySendQuotationEmailAndMarkSentAsync(uqid, ct).ConfigureAwait(false);
                if (!ok)
                {
                    TempData["QuotationError"] = err;
                    return RedirectToQuotationListFromModel(model);
                }
                TempData["QuotationMessage"] = "Quotation updated and emailed to the customer. Status: Sent.";
                return RedirectToQuotationListFromModel(model);
            }
            TempData["QuotationMessage"] = isDraftSubmit
                ? "Quotation updated (draft)."
                : "Quotation sent to customer (updated).";
            return RedirectToQuotationListFromModel(model);
        }

        string number;
        try
        {
            number = await _documentNumbers.AllocateAsync(ModuleKeys.SaleQuotation, ct).ConfigureAwait(false);
        }
        catch (DocumentIntegrationMissingException ex)
        {
            TempData["QuotationError"] = ex.Message;
            return RedirectToQuotationListFromModel(model);
        }
        catch (DocumentIntegrationRangeExhaustedException ex)
        {
            TempData["QuotationError"] = ex.Message;
            return RedirectToQuotationListFromModel(model);
        }

        var header = new SalesQuotation
        {
            QuotationNumber = number,
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
            QuotationDate = qd,
            ValidityDate = vd,
            Status = isDraftSubmit
                ? SalesQuotation.StatusDraft
                : (isSendSubmit ? SalesQuotation.StatusSent : SalesQuotation.StatusDraft)
        };
        foreach (var li in lineEntities) header.Items.Add(li);

        _db.SalesQuotations.Add(header);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        if (isEmailSubmit)
        {
            var (ok, err) = await TrySendQuotationEmailAndMarkSentAsync(header.Id, ct).ConfigureAwait(false);
            if (!ok)
            {
                TempData["QuotationError"] = err;
                return RedirectToQuotationListFromModel(model);
            }
            TempData["QuotationMessage"] = "Quotation saved and emailed to the customer. Status: Sent.";
            return RedirectToQuotationListFromModel(model);
        }
        TempData["QuotationMessage"] = isDraftSubmit
            ? "Quotation saved as draft."
            : "Quotation sent to customer (recorded).";
        return RedirectToQuotationListFromModel(model);
    }

    [HttpGet]
    public async Task<JsonResult> QuotationData(int? id, CancellationToken ct = default)
    {
        if (id is not > 0)
            return Json(new { success = false, message = "Invalid id." });
        var q = await _db.SalesQuotations.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);
        if (q == null)
            return Json(new { success = false, message = "Quotation not found." });
        if (q.Status != SalesQuotation.StatusDraft)
            return Json(new { success = false, message = "Only draft quotations can be opened for editing." });

        object? schemaCharges = null;
        if (q.ConfigurationSchemaId is int sch && sch > 0)
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
            h = new
            {
                id = q.Id,
                q.QuotationNumber,
                plantId = q.PlantId,
                distributionChannelId = q.DistributionChannelId,
                configurationSchemaId = q.ConfigurationSchemaId,
                customerBusinessPartnerId = q.CustomerBusinessPartnerId,
                customerName = q.CustomerName,
                shipToAddress = q.ShipToAddress,
                itemChargeColumnIds = q.ItemChargeColumnIds,
                quotationLevelChargeIds = q.QuotationLevelChargeIds,
                quotationChargeValuesJson = q.QuotationChargeValuesJson,
                quotationDate = q.QuotationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                validityDate = q.ValidityDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            },
            items = q.Items.OrderBy(x => x.Id).Select(i => new
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
    public async Task<IActionResult> Update(int id, SalesQuotationCreateFormModel model, CancellationToken ct = default)
    {
        return await Create(model, ct, updateQuotationId: id).ConfigureAwait(false);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken ct = default)
    {
        var q = await LoadQuotationForPdfAsync(id, ct).ConfigureAwait(false);
        if (q == null)
            return NotFound();
        var companyHeader = await _companyInfo.GetPdfHeaderAsync(ct).ConfigureAwait(false);
        var bytes = SalesQuotationPdfService.BuildPdf(q, q.Items.ToList(), companyHeader);
        var fileName = SafePdfFileName(q.QuotationNumber);
        return File(bytes, "application/pdf", fileName);
    }

    /// <summary>Read-only details for list “View” modal (sent quotations only; no layout).</summary>
    [HttpGet]
    public async Task<IActionResult> DetailsModal(int id, CancellationToken ct = default)
    {
        var o = await _db.SalesQuotations.AsNoTracking()
            .Include(x => x.Plant)
            .Include(x => x.DistributionChannel)
            .Include(x => x.ConfigurationSchema)
            .Include(x => x.Items)!.ThenInclude(i => i.QuantityUom)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);
        if (o == null)
            return NotFound();
        if (o.Status != SalesQuotation.StatusSent)
            return NotFound();
        var full = await SalesDocumentDetailsModalBuilder.BuildQuotationAsync(o, _db, ct).ConfigureAwait(false);
        return PartialView("_DetailsModal", full);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmailToCustomer(
        int id,
        [FromForm] string? returnQ,
        [FromForm] string? returnStatus,
        [FromForm] string? returnPlantId,
        [FromForm] int? returnDistributionChannelId,
        CancellationToken ct = default)
    {
        var (ok, err) = await TrySendQuotationEmailAndMarkSentAsync(id, ct).ConfigureAwait(false);
        if (!ok)
        {
            TempData["QuotationError"] = err;
            return RedirectToQuotationList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
        }
        TempData["QuotationMessage"] = "Quotation emailed to the customer. Status: Sent.";
        return RedirectToQuotationList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
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
        var q = await _db.SalesQuotations
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);
        if (q == null)
        {
            TempData["QuotationError"] = "Quotation not found.";
            return RedirectToQuotationList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
        }
        _db.SalesQuotationItems.RemoveRange(q.Items);
        _db.SalesQuotations.Remove(q);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        TempData["QuotationMessage"] = "Quotation deleted.";
        return RedirectToQuotationList(returnQ, returnStatus, returnPlantId, returnDistributionChannelId);
    }

    private IActionResult RedirectToQuotationList(
        string? returnQ, string? returnStatus, string? returnPlantId, int? returnDistributionChannelId) =>
        RedirectToAction(nameof(Index), new
        {
            q = string.IsNullOrWhiteSpace(returnQ) ? null : returnQ,
            status = string.IsNullOrWhiteSpace(returnStatus) ? null : returnStatus,
            plantId = string.IsNullOrWhiteSpace(returnPlantId) ? null : returnPlantId,
            distributionChannelId = returnDistributionChannelId
        });

    private IActionResult RedirectToQuotationListFromModel(SalesQuotationCreateFormModel? m) =>
        RedirectToQuotationList(m?.ReturnQ, m?.ReturnStatus, m?.ReturnPlantId, m?.ReturnDistributionChannelId);

    private async Task<(bool Ok, string? ErrorMessage)> TrySendQuotationEmailAndMarkSentAsync(int quotationId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_configuration["Email:SmtpHost"]))
        {
            return (false, "Email is not configured. Set Email:SmtpHost, SmtpUser, and related settings in appsettings (see Email section).");
        }
        var header = await _db.SalesQuotations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == quotationId, ct)
            .ConfigureAwait(false);
        if (header == null)
            return (false, "Quotation not found.");
        if (string.IsNullOrWhiteSpace(header.CustomerBusinessPartnerId))
        {
            return (false, "This quotation has no customer (sold-to) selected. Choose a customer before sending email.");
        }
        var bp = await _db.BusinessPartnerMasterSamples.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BPID == header.CustomerBusinessPartnerId, ct)
            .ConfigureAwait(false);
        if (bp == null)
            return (false, "Customer business partner not found.");
        var toEmail = (bp.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return (false, "The customer has no email address. Add it on the business partner (Communication tab) before sending.");
        }
        try
        {
            _ = new MailAddress(toEmail);
        }
        catch
        {
            return (false, "The customer's email address is not valid. Update it on the business partner (Communication).");
        }
        var q = await LoadQuotationForPdfAsync(quotationId, ct).ConfigureAwait(false);
        if (q == null)
            return (false, "Quotation not found.");
        var companyPdfHeader = await _companyInfo.GetPdfHeaderAsync(ct).ConfigureAwait(false);
        var pdf = SalesQuotationPdfService.BuildPdf(q, q.Items.ToList(), companyPdfHeader);
        var fileName = SafePdfFileName(q.QuotationNumber);
        var subjectFormat = _configuration["SalesQuotation:EmailSubjectFormat"] ?? "Sales quotation {0}";
        var subject = string.Format(CultureInfo.InvariantCulture, subjectFormat, q.QuotationNumber);
        var cust = WebUtility.HtmlEncode((bp.FullName ?? q.CustomerName ?? "Customer").Trim());
        var fromN = WebUtility.HtmlEncode(_configuration["Email:FromName"] ?? "AU ERP");
        var bodyTemplate = _configuration["SalesQuotation:EmailBodyTemplate"]
            ?? "<p>Dear {0},</p><p>Please find your sales quotation attached as a PDF.</p><p>Kind regards,<br/>{1}</p>";
        var htmlBody = string.Format(CultureInfo.InvariantCulture, bodyTemplate, cust, fromN);
        try
        {
            await _emailService
                .SendHtmlEmailWithAttachmentAsync(toEmail, subject, htmlBody, fileName, pdf, "application/pdf")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return (false, "Email could not be sent: " + ex.Message);
        }
        var tracked = await _db.SalesQuotations
            .FirstOrDefaultAsync(x => x.Id == quotationId, ct)
            .ConfigureAwait(false);
        if (tracked != null)
        {
            tracked.Status = SalesQuotation.StatusSent;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        return (true, null);
    }

    private async Task<SalesQuotation?> LoadQuotationForPdfAsync(int id, CancellationToken ct) =>
        await _db.SalesQuotations.AsNoTracking()
            .Include(x => x.Items).ThenInclude(i => i.QuantityUom)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

    private static string SafePdfFileName(string quotationNumber)
    {
        var s = (quotationNumber ?? "quotation").Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '-');
        if (string.IsNullOrEmpty(s)) s = "quotation";
        if (!s.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) s += ".pdf";
        return s;
    }

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

    private async Task ApplyCustomerDrivenDefaultsAsync(SalesQuotationCreateFormModel model, BusinessPartnerMasterSample bp, CancellationToken ct)
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
}
