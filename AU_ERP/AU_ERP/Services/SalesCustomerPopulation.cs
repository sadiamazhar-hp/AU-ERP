using System.Security.Claims;
using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>How SQ/SO customer dropdowns are scoped from user plant assignments.</summary>
public enum SalesCustomerPopulationMode
{
    WalkInOnly,
    DealerOnly,
    WalkInAndDealer
}

/// <summary>Resolves SQ/SO customer population from assigned operational plants and sales schemas.</summary>
public static class SalesCustomerPopulation
{
    public static SalesCustomerPopulationMode ResolveMode(
        IReadOnlyList<string> assignedPlantIds,
        bool isAdminAllPlants = false)
    {
        if (isAdminAllPlants)
            return SalesCustomerPopulationMode.WalkInAndDealer;

        var plants = NormalizePlantIds(assignedPlantIds);
        if (plants.Count == 0)
            return SalesCustomerPopulationMode.DealerOnly;

        var hasEmporium = plants.Contains(UserPlantResolution.EmporiumPlantId, StringComparer.OrdinalIgnoreCase);
        var hasManufacturing = plants.Contains(UserPlantResolution.ManufacturingPlantId, StringComparer.OrdinalIgnoreCase);
        var hasOther = plants.Any(p =>
            !string.Equals(p, UserPlantResolution.EmporiumPlantId, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(p, UserPlantResolution.ManufacturingPlantId, StringComparison.OrdinalIgnoreCase));

        if (hasEmporium && hasManufacturing && !hasOther)
            return SalesCustomerPopulationMode.WalkInAndDealer;

        if (hasEmporium && !hasManufacturing && !hasOther)
            return SalesCustomerPopulationMode.WalkInOnly;

        return SalesCustomerPopulationMode.DealerOnly;
    }

    public static async Task<SalesCustomerPopulationMode> ResolveModeForUserAsync(
        AppDbContext db,
        ClaimsPrincipal? user,
        IEnumerable<string> allPlantIdsFromDb,
        CancellationToken ct = default)
    {
        var all = allPlantIdsFromDb
            .Select(id => (id ?? "").Trim())
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var assigned = await SalesPlantAccess.LoadAssignedPlantIdsAsync(db, user, all, ct).ConfigureAwait(false);
        var isAdminAllPlants = UserPlantResolution.IsAdminDepartment(user)
            && !SalesPlantAccess.HasOperationalDepartment(user)
            && assigned.Count == 0;
        return ResolveMode(assigned, isAdminAllPlants);
    }

    public static IQueryable<BusinessPartnerMasterSample> ApplyFilter(
        IQueryable<BusinessPartnerMasterSample> query,
        SalesCustomerPopulationMode mode,
        int? walkInSchemaId,
        int? dealerSchemaId)
    {
        switch (mode)
        {
            case SalesCustomerPopulationMode.WalkInOnly:
                return walkInSchemaId is > 0
                    ? SalesSchemaResolution.FilterWalkInCustomers(query, walkInSchemaId.Value)
                    : query.Where(_ => false);
            case SalesCustomerPopulationMode.DealerOnly:
                return dealerSchemaId is > 0
                    ? SalesSchemaResolution.FilterDealerCustomers(query, dealerSchemaId.Value)
                    : query;
            case SalesCustomerPopulationMode.WalkInAndDealer:
                if (walkInSchemaId is > 0 && dealerSchemaId is > 0)
                    return SalesSchemaResolution.FilterWalkInAndDealerCustomers(query, walkInSchemaId.Value, dealerSchemaId.Value);
                if (walkInSchemaId is > 0)
                    return SalesSchemaResolution.FilterWalkInCustomers(query, walkInSchemaId.Value);
                if (dealerSchemaId is > 0)
                    return SalesSchemaResolution.FilterDealerCustomers(query, dealerSchemaId.Value);
                return query.Where(_ => false);
            default:
                return query;
        }
    }

    public static bool IsWalkInOnlyUi(SalesCustomerPopulationMode mode, int? walkInSchemaId) =>
        mode == SalesCustomerPopulationMode.WalkInOnly && walkInSchemaId is > 0;

    public static bool CanCreateWalkInCustomer(IReadOnlyList<string> assignedPlantIds) =>
        assignedPlantIds.Contains(UserPlantResolution.EmporiumPlantId, StringComparer.OrdinalIgnoreCase);

    /// <summary>True when SQ/SO should show the walk-in "Add customer" control (any Emporium assignment).</summary>
    public static bool ShowAddWalkInCustomer(IReadOnlyList<string> assignedPlantIds) =>
        CanCreateWalkInCustomer(assignedPlantIds);

    public static string? ValidateCustomer(
        BusinessPartnerMasterSample bp,
        SalesCustomerPopulationMode mode,
        int? walkInSchemaId,
        int? dealerSchemaId,
        string? plantId,
        string docLabel)
    {
        var plant = (plantId ?? string.Empty).Trim();
        if (string.Equals(plant, UserPlantResolution.EmporiumPlantId, StringComparison.OrdinalIgnoreCase))
        {
            if (walkInSchemaId is not > 0)
                return null;
            if (!SalesSchemaResolution.MatchesWalkInSchema(bp.SalesSchema, walkInSchemaId.Value))
                return $"Only WalkIn customers can be used for Emporium {docLabel}.";
            return null;
        }

        if (plant.Length > 0)
        {
            if (dealerSchemaId is not > 0)
                return null;
            if (!SalesSchemaResolution.MatchesDealerSchema(bp.SalesSchema, dealerSchemaId.Value))
                return $"Only Dealer customers can be used for {docLabel} outside Emporium plant.";
            return null;
        }

        switch (mode)
        {
            case SalesCustomerPopulationMode.WalkInOnly:
                if (walkInSchemaId is not > 0)
                    return null;
                if (!SalesSchemaResolution.MatchesWalkInSchema(bp.SalesSchema, walkInSchemaId.Value))
                    return $"Only WalkIn customers can be used for Emporium {docLabel}.";
                return null;
            case SalesCustomerPopulationMode.DealerOnly:
                if (dealerSchemaId is not > 0)
                    return null;
                if (!SalesSchemaResolution.MatchesDealerSchema(bp.SalesSchema, dealerSchemaId.Value))
                    return $"Only Dealer customers can be used for {docLabel} outside Emporium plant.";
                return null;
            case SalesCustomerPopulationMode.WalkInAndDealer:
                var walkOk = walkInSchemaId is > 0
                    && SalesSchemaResolution.MatchesWalkInSchema(bp.SalesSchema, walkInSchemaId.Value);
                var dealerOk = dealerSchemaId is > 0
                    && SalesSchemaResolution.MatchesDealerSchema(bp.SalesSchema, dealerSchemaId.Value);
                if (walkOk || dealerOk)
                    return null;
                return $"Customer must be WalkIn or Dealer for your assigned plants on {docLabel}.";
            default:
                return null;
        }
    }

    private static List<string> NormalizePlantIds(IReadOnlyList<string> assignedPlantIds) =>
        assignedPlantIds
            .Select(p => (p ?? "").Trim())
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
