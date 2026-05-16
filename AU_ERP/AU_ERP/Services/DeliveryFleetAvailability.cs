using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>
/// Drivers/vehicles are busy while tied to an active delivery challan: no linked sales invoice yet, or invoice
/// <see cref="SalesInvoice.StatusOpen"/> / <see cref="SalesInvoice.StatusReturnInProcess"/>, unless the challan is
/// marked complete via <see cref="DeliveryChallan.DeliveryCompletedAt"/>.
/// </summary>
public static class DeliveryFleetAvailability
{
    public static bool InvoiceStatusBlocksFleetReuse(string? status)
    {
        if (string.IsNullOrEmpty(status))
            return false;
        return string.Equals(status, SalesInvoice.StatusOpen, StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, SalesInvoice.StatusReturnInProcess, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Driver ids on an unfinished delivery challan (see class summary).</summary>
    public static async Task<HashSet<int>> GetDriverIdsBusyOnOpenInvoiceAsync(AppDbContext db, CancellationToken ct)
    {
        var ids = await db.DeliveryChallans.AsNoTracking()
            .Where(dc => dc.DriverId != null && dc.DeliveryCompletedAt == null)
            .Where(dc => !db.SalesInvoices.Any(i => i.DeliveryChallanId == dc.Id)
                         || db.SalesInvoices.Any(i =>
                             i.DeliveryChallanId == dc.Id
                             && (i.Status == SalesInvoice.StatusOpen || i.Status == SalesInvoice.StatusReturnInProcess)))
            .Select(dc => dc.DriverId!.Value)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return ids.ToHashSet();
    }

    /// <summary>Vehicle ids on an unfinished delivery challan (see class summary).</summary>
    public static async Task<HashSet<int>> GetVehicleIdsBusyOnOpenInvoiceAsync(AppDbContext db, CancellationToken ct)
    {
        var ids = await db.DeliveryChallans.AsNoTracking()
            .Where(dc => dc.VehicleId != null && dc.DeliveryCompletedAt == null)
            .Where(dc => !db.SalesInvoices.Any(i => i.DeliveryChallanId == dc.Id)
                         || db.SalesInvoices.Any(i =>
                             i.DeliveryChallanId == dc.Id
                             && (i.Status == SalesInvoice.StatusOpen || i.Status == SalesInvoice.StatusReturnInProcess)))
            .Select(dc => dc.VehicleId!.Value)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return ids.ToHashSet();
    }

    private static string OrderLabel(string? referenceSo, string? salesOrderNumber, string dcNumber)
    {
        var r = string.IsNullOrWhiteSpace(referenceSo) ? "" : referenceSo.Trim();
        if (r.Length > 0) return r.Length <= 40 ? r : r[..40];
        var s = string.IsNullOrWhiteSpace(salesOrderNumber) ? "" : salesOrderNumber.Trim();
        if (s.Length > 0) return s.Length <= 40 ? s : s[..40];
        var d = string.IsNullOrWhiteSpace(dcNumber) ? "" : dcNumber.Trim();
        return string.IsNullOrEmpty(d) ? "—" : ("DC " + (d.Length <= 32 ? d : d[..32]));
    }

    public static async Task<Dictionary<int, string>> GetDriverBusyOrderLabelsAsync(AppDbContext db, CancellationToken ct)
    {
        var rows = await (
                from dc in db.DeliveryChallans.AsNoTracking()
                join so in db.SalesOrders.AsNoTracking() on dc.SalesOrderId equals so.Id into soGrp
                from so in soGrp.DefaultIfEmpty()
                where dc.DriverId != null && dc.DeliveryCompletedAt == null
                where !db.SalesInvoices.Any(i => i.DeliveryChallanId == dc.Id)
                      || db.SalesInvoices.Any(i =>
                          i.DeliveryChallanId == dc.Id
                          && (i.Status == SalesInvoice.StatusOpen || i.Status == SalesInvoice.StatusReturnInProcess))
                select new
                {
                    DriverId = dc.DriverId!.Value,
                    dc.ReferenceSalesOrderNumber,
                    SalesNo = so != null ? so.SalesOrderNumber : null,
                    dc.DeliveryChallanNumber
                })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var map = new Dictionary<int, string>();
        foreach (var g in rows.GroupBy(r => r.DriverId))
        {
            var parts = g
                .Select(x => OrderLabel(x.ReferenceSalesOrderNumber, x.SalesNo, x.DeliveryChallanNumber))
                .Where(s => s.Length > 0 && s != "—")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();
            map[g.Key] = parts.Count > 0 ? string.Join("; ", parts) : "—";
        }

        return map;
    }

    public static async Task<Dictionary<int, string>> GetVehicleBusyOrderLabelsAsync(AppDbContext db, CancellationToken ct)
    {
        var rows = await (
                from dc in db.DeliveryChallans.AsNoTracking()
                join so in db.SalesOrders.AsNoTracking() on dc.SalesOrderId equals so.Id into soGrp
                from so in soGrp.DefaultIfEmpty()
                where dc.VehicleId != null && dc.DeliveryCompletedAt == null
                where !db.SalesInvoices.Any(i => i.DeliveryChallanId == dc.Id)
                      || db.SalesInvoices.Any(i =>
                          i.DeliveryChallanId == dc.Id
                          && (i.Status == SalesInvoice.StatusOpen || i.Status == SalesInvoice.StatusReturnInProcess))
                select new
                {
                    VehicleId = dc.VehicleId!.Value,
                    dc.ReferenceSalesOrderNumber,
                    SalesNo = so != null ? so.SalesOrderNumber : null,
                    dc.DeliveryChallanNumber
                })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var map = new Dictionary<int, string>();
        foreach (var g in rows.GroupBy(r => r.VehicleId))
        {
            var parts = g
                .Select(x => OrderLabel(x.ReferenceSalesOrderNumber, x.SalesNo, x.DeliveryChallanNumber))
                .Where(s => s.Length > 0 && s != "—")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();
            map[g.Key] = parts.Count > 0 ? string.Join("; ", parts) : "—";
        }

        return map;
    }
}
