using AU_ERP.Configuration;
using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>
/// Highest numeric suffix already persisted for issuing tables keyed by Document Integration (<c>PREFIX-nnn</c>).
/// Loads candidates with a StartsWith(prefix) filter, then parses in-process.
/// </summary>
public static class IntegratedDocumentIssuedNumbersMaxSuffix
{
    public static async Task<long> GetMaxSuffixAsync(
        AppDbContext db,
        string docCode,
        string moduleKey,
        CancellationToken ct = default)
    {
        docCode = (docCode ?? "").Trim();
        if (docCode.Length == 0)
            return 0;

        var prefix = docCode + "-";

        switch (moduleKey)
        {
            case ModuleKeys.SaleQuotation:
            {
                var rows = await db.SalesQuotations.AsNoTracking()
                    .Where(q => q.QuotationNumber.StartsWith(prefix))
                    .Select(q => q.QuotationNumber)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.SaleOrder:
            {
                var rows = await db.SalesOrders.AsNoTracking()
                    .Where(o => o.SalesOrderNumber.StartsWith(prefix))
                    .Select(o => o.SalesOrderNumber)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.SalesGoodsIssue:
            {
                var rows = await db.SalesGoodsIssueDocuments.AsNoTracking()
                    .Where(g => g.DocumentNumber.StartsWith(prefix))
                    .Select(g => g.DocumentNumber)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.StockGoodsIssue:
            {
                var rows = await db.GoodsIssueDocuments.AsNoTracking()
                    .Where(g => g.DocumentNumber.StartsWith(prefix))
                    .Select(g => g.DocumentNumber)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.DeliveryChallan:
            {
                var rows = await db.DeliveryChallans.AsNoTracking()
                    .Where(d => d.DeliveryChallanNumber.StartsWith(prefix))
                    .Select(d => d.DeliveryChallanNumber)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.Invoice:
            {
                var rows = await db.SalesInvoices.AsNoTracking()
                    .Where(i => i.DocumentNumber.StartsWith(prefix))
                    .Select(i => i.DocumentNumber)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.Payment:
            {
                var rows = await db.SalesPayments.AsNoTracking()
                    .Where(p => p.DocumentNumber.StartsWith(prefix))
                    .Select(p => p.DocumentNumber)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.ReturnOrder:
            {
                var rows = await db.SalesReturnOrders.AsNoTracking()
                    .Where(r => r.DocumentNumber.StartsWith(prefix))
                    .Select(r => r.DocumentNumber)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.CreditMemo:
            {
                var rows = await db.SalesReturnCreditMemos.AsNoTracking()
                    .Where(c => c.DocumentNumber.StartsWith(prefix))
                    .Select(c => c.DocumentNumber)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.ProductionOrder:
            {
                var rows = await db.ProductionOrders.AsNoTracking()
                    .Where(p =>
                        p.ProductionDocumentNumber != null
                        && p.ProductionDocumentNumber.StartsWith(prefix))
                    .Select(p => p.ProductionDocumentNumber!)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.QualityInspection:
            {
                var rows = await db.GoodReceiptDocuments.AsNoTracking()
                    .Where(g => g.DocumentNumber.StartsWith(prefix))
                    .Select(g => g.DocumentNumber)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.Batch:
            {
                var rows = await db.GoodReceiptDocuments.AsNoTracking()
                    .Where(g => g.BatchNo.StartsWith(prefix))
                    .Select(g => g.BatchNo)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                return DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(rows, docCode);
            }
            case ModuleKeys.ReservationIssue:
            default:
                return 0;
        }
    }
}
