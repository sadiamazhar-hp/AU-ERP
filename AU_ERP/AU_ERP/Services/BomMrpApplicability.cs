using AU_ERP.Models;

namespace AU_ERP.Services;

/// <summary>MRP-only BOM applicability (non-deleted, active, ValidFrom-only).</summary>
public static class BomMrpApplicability
{
    public static IQueryable<BomHeadersSample> ForMrpSelection(
        this IQueryable<BomHeadersSample> q,
        DateTime asOfDate)
    {
        var day = asOfDate.Date;
        return q.Where(h => !h.IsDeleted
            && h.Status == "Active"
            && (!h.ValidFrom.HasValue || h.ValidFrom.Value.Date <= day));
    }

    public static IOrderedQueryable<BomHeadersSample> OrderForMrpSelection(
        this IQueryable<BomHeadersSample> q) =>
        q.OrderByDescending(h => h.IsDefaultBom)
            .ThenByDescending(h => h.ValidFrom)
            .ThenByDescending(h => h.BomID);

    /// <summary>Non-deleted BOM headers for master maintenance dropdowns.</summary>
    public static IQueryable<BomHeadersSample> ActiveMaster(
        this IQueryable<BomHeadersSample> q) =>
        q.Where(h => !h.IsDeleted);
}
