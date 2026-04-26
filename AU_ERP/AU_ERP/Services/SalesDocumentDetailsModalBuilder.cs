using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>Builds read-only charge breakdowns for list “View” modals (same rules as <see cref="SalesQuotationPricing"/> and create flows).</summary>
public static class SalesDocumentDetailsModalBuilder
{
    public static async Task<SalesOrderDetailsFullVm> BuildSalesOrderAsync(SalesOrder order, AppDbContext db, CancellationToken ct = default)
    {
        var itemColList = SalesQuotationPricing.ParseIdList(order.ItemChargeColumnIds);
        var docOrder = SalesQuotationPricing.ParseIdList(order.QuotationLevelChargeIds);
        var docValues = SalesQuotationPricing.ParseChargeValuesJson(order.QuotationChargeValuesJson);

        var idSet = new HashSet<int>(itemColList);
        idSet.UnionWith(docOrder);
        foreach (var li in order.Items ?? Array.Empty<SalesOrderItem>())
        {
            if (li.LineTaxChargeId is { } t && t > 0) idSet.Add(t);
            idSet.UnionWith(SalesQuotationPricing.ParseIdList(li.ItemAppliedChargeIds));
            idSet.UnionWith(SalesQuotationPricing.ParseChargeValuesJson(li.ItemChargeValuesJson).Keys);
        }

        var chargeById = idSet.Count == 0
            ? new Dictionary<int, Charge>()
            : await db.Charges.AsNoTracking()
                .Where(c => idSet.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, ct)
                .ConfigureAwait(false);

        var lineDisplays = new List<SalesLineDisplayVm>();
        foreach (var li in (order.Items ?? Array.Empty<SalesOrderItem>()).OrderBy(i => i.Id))
        {
            var lineVals = SalesQuotationPricing.ParseChargeValuesJson(li.ItemChargeValuesJson);
            lineDisplays.Add(new SalesLineDisplayVm
            {
                MaterialNumber = li.MaterialNumber,
                MaterialDescription = li.MaterialDescription,
                UomCode = li.QuantityUom?.Code,
                Qty = li.OrderQuantity,
                UnitPrice = li.UnitPrice,
                SubtotalBeforeItemCharges = li.SubtotalAfterDiscount,
                ItemChargeValues = lineVals,
                LineNet = li.NetPrice
            });
        }

        var itemHeaders = itemColList
            .Where(id => chargeById.ContainsKey(id))
            .Select(id => chargeById[id])
            .Select(c => new SalesLineChargeColVm
            {
                ChargeId = c.Id,
                Symbol = c.Symbol,
                IsPercentage = c.ValueType == Charge.TypePercentage
            })
            .ToList();

        var lineSum = lineDisplays.Sum(l => l.LineNet);
        var (docSteps, grand) = BuildDocumentSteps(docOrder, chargeById, docValues, lineSum);

        return new SalesOrderDetailsFullVm
        {
            Order = order,
            Pricing = new SalesDocumentDetailsModalVm
            {
                ItemChargeColumns = itemHeaders,
                Lines = lineDisplays,
                SubtotalLineTotals = Math.Round(lineSum, 4, MidpointRounding.AwayFromZero),
                DocumentChargeSteps = docSteps,
                GrandTotal = grand
            }
        };
    }

    public static async Task<SalesQuotationDetailsFullVm> BuildQuotationAsync(SalesQuotation quotation, AppDbContext db, CancellationToken ct = default)
    {
        var itemColList = SalesQuotationPricing.ParseIdList(quotation.ItemChargeColumnIds);
        var docOrder = SalesQuotationPricing.ParseIdList(quotation.QuotationLevelChargeIds);
        var docValues = SalesQuotationPricing.ParseChargeValuesJson(quotation.QuotationChargeValuesJson);

        var idSet = new HashSet<int>(itemColList);
        idSet.UnionWith(docOrder);
        foreach (var li in quotation.Items ?? Array.Empty<SalesQuotationItem>())
        {
            if (li.LineTaxChargeId is { } t && t > 0) idSet.Add(t);
            idSet.UnionWith(SalesQuotationPricing.ParseIdList(li.ItemAppliedChargeIds));
            idSet.UnionWith(SalesQuotationPricing.ParseChargeValuesJson(li.ItemChargeValuesJson).Keys);
        }

        var chargeById = idSet.Count == 0
            ? new Dictionary<int, Charge>()
            : await db.Charges.AsNoTracking()
                .Where(c => idSet.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, ct)
                .ConfigureAwait(false);

        var lineDisplays = new List<SalesLineDisplayVm>();
        foreach (var li in (quotation.Items ?? Array.Empty<SalesQuotationItem>()).OrderBy(i => i.Id))
        {
            var lineVals = SalesQuotationPricing.ParseChargeValuesJson(li.ItemChargeValuesJson);
            lineDisplays.Add(new SalesLineDisplayVm
            {
                MaterialNumber = li.MaterialNumber,
                MaterialDescription = li.MaterialDescription,
                UomCode = li.QuantityUom?.Code,
                Qty = li.OrderQuantity,
                UnitPrice = li.UnitPrice,
                SubtotalBeforeItemCharges = li.SubtotalAfterDiscount,
                ItemChargeValues = lineVals,
                LineNet = li.NetPrice
            });
        }

        var itemHeaders = itemColList
            .Where(id => chargeById.ContainsKey(id))
            .Select(id => chargeById[id])
            .Select(c => new SalesLineChargeColVm
            {
                ChargeId = c.Id,
                Symbol = c.Symbol,
                IsPercentage = c.ValueType == Charge.TypePercentage
            })
            .ToList();

        var lineSum = lineDisplays.Sum(l => l.LineNet);
        var (docSteps, grand) = BuildDocumentSteps(docOrder, chargeById, docValues, lineSum);

        return new SalesQuotationDetailsFullVm
        {
            Quotation = quotation,
            Pricing = new SalesDocumentDetailsModalVm
            {
                ItemChargeColumns = itemHeaders,
                Lines = lineDisplays,
                SubtotalLineTotals = Math.Round(lineSum, 4, MidpointRounding.AwayFromZero),
                DocumentChargeSteps = docSteps,
                GrandTotal = grand
            }
        };
    }

    private static (IReadOnlyList<SalesDocChargeStepVm> steps, decimal grand) BuildDocumentSteps(
        IReadOnlyList<int> docOrder,
        IReadOnlyDictionary<int, Charge> byId,
        IReadOnlyDictionary<int, decimal> docValues,
        decimal lineSum)
    {
        var running = lineSum;
        var steps = new List<SalesDocChargeStepVm>();
        foreach (var cid in docOrder)
        {
            if (!byId.TryGetValue(cid, out var ch)) continue;
            var has = docValues.ContainsKey(cid);
            decimal eff;
            if (ch.ValueType == Charge.TypePercentage)
            {
                eff = SalesQuotationPricing.ResolvePercentageInput(ch, docValues, cid, has);
                if (eff == 0) continue;
            }
            else
            {
                eff = SalesQuotationPricing.ResolveNumericInput(docValues, cid, has);
                if (eff == 0) continue;
            }

            var before = running;
            var after = SalesQuotationPricing.ApplyUserChargeValue(ch, running, eff);
            after = Math.Round(after, 4, MidpointRounding.AwayFromZero);
            steps.Add(new SalesDocChargeStepVm
            {
                ChargeId = cid,
                Symbol = ch.Symbol,
                Description = ch.Description,
                IsPercentage = ch.ValueType == Charge.TypePercentage,
                InputValue = eff,
                Delta = Math.Round(after - before, 4, MidpointRounding.AwayFromZero),
                RunningAfter = after
            });
            running = after;
        }

        return (steps, running);
    }
}
