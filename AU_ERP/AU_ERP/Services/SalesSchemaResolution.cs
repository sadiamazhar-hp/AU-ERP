using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>Resolves and filters business partners by WalkIn / Dealer sales configuration schemas.</summary>
public static class SalesSchemaResolution
{
    public const string WalkInSchemaTitle = EmporiumWalkInCustomerService.WalkInSchemaTitle;
    public const string DealerSchemaTitle = "Dealer";

    public static async Task<int?> GetWalkInSchemaIdAsync(AppDbContext db, CancellationToken ct = default) =>
        await GetSchemaIdByTitleAsync(db, WalkInSchemaTitle, ct);

    public static async Task<int?> GetDealerSchemaIdAsync(AppDbContext db, CancellationToken ct = default) =>
        await GetSchemaIdByTitleAsync(db, DealerSchemaTitle, ct);

    private static async Task<int?> GetSchemaIdByTitleAsync(AppDbContext db, string title, CancellationToken ct) =>
        await db.ConfigurationSchemas.AsNoTracking()
            .Where(s => s.SchemaType == ConfigurationSchemaType.Sales
                        && s.Title.ToLower() == title.ToLower())
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    /// <summary>True when BP stores WalkIn as schema ID or legacy title.</summary>
    public static bool MatchesWalkInSchema(string? bpSalesSchema, int walkInSchemaId)
    {
        var raw = (bpSalesSchema ?? string.Empty).Trim();
        if (raw.Length == 0 || walkInSchemaId <= 0)
            return false;
        if (int.TryParse(raw, out var byId) && byId == walkInSchemaId)
            return true;
        return raw.Equals(WalkInSchemaTitle, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when BP stores Dealer as schema ID or legacy title.</summary>
    public static bool MatchesDealerSchema(string? bpSalesSchema, int dealerSchemaId)
    {
        var raw = (bpSalesSchema ?? string.Empty).Trim();
        if (raw.Length == 0 || dealerSchemaId <= 0)
            return false;
        if (int.TryParse(raw, out var byId) && byId == dealerSchemaId)
            return true;
        return raw.Equals(DealerSchemaTitle, StringComparison.OrdinalIgnoreCase);
    }

    public static IQueryable<BusinessPartnerMasterSample> FilterWalkInCustomers(
        IQueryable<BusinessPartnerMasterSample> query,
        int walkInSchemaId)
    {
        var idStr = walkInSchemaId.ToString();
        return query.Where(c => c.SalesSchema == idStr
                                || c.SalesSchema == WalkInSchemaTitle);
    }

    public static IQueryable<BusinessPartnerMasterSample> FilterDealerCustomers(
        IQueryable<BusinessPartnerMasterSample> query,
        int dealerSchemaId)
    {
        var idStr = dealerSchemaId.ToString();
        return query.Where(c => c.SalesSchema == idStr
                                || c.SalesSchema == DealerSchemaTitle);
    }

    public static IQueryable<BusinessPartnerMasterSample> FilterWalkInAndDealerCustomers(
        IQueryable<BusinessPartnerMasterSample> query,
        int walkInSchemaId,
        int dealerSchemaId)
    {
        var walkInIdStr = walkInSchemaId.ToString();
        var dealerIdStr = dealerSchemaId.ToString();
        return query.Where(c => c.SalesSchema == walkInIdStr
                                || c.SalesSchema == WalkInSchemaTitle
                                || c.SalesSchema == dealerIdStr
                                || c.SalesSchema == DealerSchemaTitle);
    }

    /// <summary>Display label for BP list (WalkIn, Dealer, or em dash when unset).</summary>
    public static string ResolveSalesSchemaDisplayTitle(
        string? bpSalesSchema,
        IReadOnlyDictionary<string, string> idToTitle)
    {
        var raw = (bpSalesSchema ?? string.Empty).Trim();
        if (raw.Length == 0)
            return "—";
        if (int.TryParse(raw, out var id) && idToTitle.TryGetValue(id.ToString(), out var title) && !string.IsNullOrWhiteSpace(title))
            return title;
        if (raw.Equals(WalkInSchemaTitle, StringComparison.OrdinalIgnoreCase))
            return WalkInSchemaTitle;
        if (raw.Equals(DealerSchemaTitle, StringComparison.OrdinalIgnoreCase))
            return DealerSchemaTitle;
        return raw;
    }
}
