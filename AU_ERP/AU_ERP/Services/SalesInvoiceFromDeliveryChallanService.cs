using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>Builds a <see cref="SalesInvoice"/> from a persisted <see cref="DeliveryChallan"/> and adds it to the context (caller saves).</summary>
public sealed class SalesInvoiceFromDeliveryChallanService
{
    private readonly AppDbContext _db;

    public SalesInvoiceFromDeliveryChallanService(AppDbContext db) => _db = db;

    public async Task AddInvoiceForDeliveryChallanAsync(
        DeliveryChallan dc,
        string invoiceDocumentNumber,
        CancellationToken ct = default)
    {
        if (dc.Id <= 0)
            throw new InvalidOperationException("Delivery challan must be saved before creating an invoice.");

        var bp = string.IsNullOrWhiteSpace(dc.ShipToBusinessPartnerId)
            ? null
            : await _db.BusinessPartnerMasterSamples.AsNoTracking()
                .FirstOrDefaultAsync(b => b.BPID == dc.ShipToBusinessPartnerId, ct)
                .ConfigureAwait(false);

        SalesOrder? so = null;
        if (dc.SalesOrderId is int soid && soid > 0)
        {
            so = await _db.SalesOrders.AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == soid, ct)
                .ConfigureAwait(false);
        }

        var termForDue = (bp?.PaymentTerms ?? "").Trim();
        if (string.IsNullOrEmpty(termForDue) && so != null)
            termForDue = (so.PaymentTerm ?? "").Trim();
        var dueDate = InvoiceDueDateHelper.ComputeDueDate(dc.DocumentDate, termForDue);
        var daysUsed = InvoiceDueDateHelper.ResolvePaymentDays(termForDue);
        var snapshot = string.IsNullOrEmpty(termForDue)
            ? $"default {daysUsed} days"
            : $"{termForDue} ({daysUsed} days)";

        var chargeIds = new HashSet<int>();
        if (so != null)
        {
            foreach (var id in SalesQuotationPricing.ParseIdList(so.ItemChargeColumnIds))
                chargeIds.Add(id);
            foreach (var id in SalesQuotationPricing.ParseIdList(so.QuotationLevelChargeIds))
                chargeIds.Add(id);
            foreach (var dci in dc.Items)
            {
                if (dci.SalesOrderItemId is not int sii || sii <= 0) continue;
                var soi = so.Items.FirstOrDefault(i => i.Id == sii);
                if (soi == null) continue;
                foreach (var id in SalesQuotationPricing.ParseIdList(soi.ItemAppliedChargeIds))
                    chargeIds.Add(id);
                if (soi.LineTaxChargeId is int tx && tx > 0)
                    chargeIds.Add(tx);
            }
        }

        IReadOnlyDictionary<int, Charge> chargeById = new Dictionary<int, Charge>();
        if (chargeIds.Count > 0)
        {
            chargeById = await _db.Charges.AsNoTracking()
                .Where(c => chargeIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, ct)
                .ConfigureAwait(false);
        }

        var itemColumnOrder = so != null
            ? SalesQuotationPricing.ParseIdList(so.ItemChargeColumnIds)
            : Array.Empty<int>();

        var lineNo = 1;
        decimal sumLineTotals = 0m;
        var invoice = new SalesInvoice
        {
            DocumentNumber = invoiceDocumentNumber,
            DocumentDate = dc.DocumentDate,
            DueDate = dueDate,
            DealerBusinessPartnerId = string.IsNullOrWhiteSpace(dc.ShipToBusinessPartnerId) ? null : dc.ShipToBusinessPartnerId,
            DealerDisplayName = string.IsNullOrWhiteSpace(dc.ShipToDisplayName)
                ? bp?.FullName
                : dc.ShipToDisplayName![..Math.Min(500, dc.ShipToDisplayName.Length)],
            DeliveryChallanId = dc.Id,
            DcNumber = dc.DeliveryChallanNumber,
            Status = SalesInvoice.StatusOpen,
            PaymentTermsSnapshot = snapshot[..Math.Min(128, snapshot.Length)],
            CreatedAt = DateTime.UtcNow,
            Lines = new List<SalesInvoiceLine>()
        };

        foreach (var dci in dc.Items.OrderBy(i => i.Id))
        {
            decimal lineSub;
            decimal lineTot;
            decimal unitPrice = 0m;
            string? itemJson = null;
            int? srcSoItem = dci.SalesOrderItemId;

            if (so != null && dci.SalesOrderItemId is int sii && sii > 0)
            {
                var soi = so.Items.FirstOrDefault(i => i.Id == sii);
                if (soi != null)
                {
                    unitPrice = soi.UnitPrice;
                    var disc = soi.DiscountPercent;
                    var lineVals = SalesQuotationPricing.ParseChargeValuesJson(soi.ItemChargeValuesJson);
                    itemJson = soi.ItemChargeValuesJson;
                    (lineSub, lineTot) = SalesQuotationPricing.ComputeLineWithChargeValues(
                        itemColumnOrder,
                        chargeById,
                        lineVals,
                        dci.DeliveryQuantity,
                        unitPrice,
                        disc);
                }
                else
                {
                    lineSub = 0m;
                    lineTot = 0m;
                    srcSoItem = null;
                }
            }
            else
            {
                lineSub = 0m;
                lineTot = 0m;
                srcSoItem = null;
            }

            sumLineTotals += lineTot;
            invoice.Lines.Add(new SalesInvoiceLine
            {
                LineNo = lineNo++,
                MaterialNumber = dci.MaterialNumber,
                MaterialDescription = dci.MaterialDescription,
                Quantity = dci.DeliveryQuantity,
                QuantityUomId = dci.QuantityUomId,
                UnitPrice = unitPrice,
                LineSubtotal = lineSub,
                LineTotal = lineTot,
                ItemChargeValuesJson = itemJson,
                SourceSalesOrderItemId = srcSoItem
            });
        }

        decimal grandTotal;
        if (so != null)
        {
            var docOrder = SalesQuotationPricing.ParseIdList(so.QuotationLevelChargeIds);
            var docVals = SalesQuotationPricing.ParseChargeValuesJson(so.QuotationChargeValuesJson);
            grandTotal = SalesQuotationPricing.ApplyQuotationChargeValues(docOrder, chargeById, docVals, sumLineTotals);
        }
        else
        {
            grandTotal = sumLineTotals;
        }

        invoice.Subtotal = Math.Round(sumLineTotals, 4, MidpointRounding.AwayFromZero);
        invoice.GrandTotal = Math.Round(grandTotal, 4, MidpointRounding.AwayFromZero);

        await _db.SalesInvoices.AddAsync(invoice, ct).ConfigureAwait(false);
    }
}
