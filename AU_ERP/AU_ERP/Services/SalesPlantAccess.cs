using System.Linq.Expressions;
using System.Security.Claims;
using AU_ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>Resolves plant scope for operational module listings and writes (uses Store/Sales/Production department <see cref="ApplicationUserDepartment.PlantID"/> assignments).</summary>
public sealed record SalesPlantScope(
    IReadOnlyList<string> AllowedPlantIds,
    string? EffectiveListPlantId,
    bool IsSinglePlantLocked,
    bool MissingAssignment,
    bool IsAdminAllPlants);

public static class SalesPlantAccess
{
    public static bool HasOperationalDepartment(ClaimsPrincipal? user) =>
        user?.HasClaim(AuClaimTypes.Department, "Store") == true
        || user?.HasClaim(AuClaimTypes.Department, "Sales") == true
        || user?.HasClaim(AuClaimTypes.Department, "Production") == true;

    /// <summary>Loads assigned plant ids from Store/Sales/Production department rows (authoritative for operational users).</summary>
    public static async Task<IReadOnlyList<string>> LoadAssignedPlantIdsAsync(
        AppDbContext db,
        ClaimsPrincipal? user,
        IEnumerable<string> allPlantIdsFromDb,
        CancellationToken ct = default)
    {
        var all = NormalizePlantIds(allPlantIdsFromDb);
        if (user == null || all.Count == 0)
            return Array.Empty<string>();

        var hasOperationalDept = HasOperationalDepartment(user);
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var csvRows = await (
                from ud in db.ApplicationUserDepartments.AsNoTracking()
                join d in db.Departments.AsNoTracking() on ud.DepartmentId equals d.Id
                where ud.UserId == userId
                    && (d.Code == "Store" || d.Code == "Sales" || d.Code == "Production")
                    && ud.PlantID != null
                    && ud.PlantID != ""
                select ud.PlantID!).ToListAsync(ct).ConfigureAwait(false);

            if (csvRows.Count > 0)
            {
                var fromDb = csvRows
                    .SelectMany(ParsePlantCsv)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var allSet = all.ToHashSet(StringComparer.OrdinalIgnoreCase);
                return fromDb.Where(p => allSet.Contains(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            // Operational users: never fall back to stale identity claims — empty DB means no assignment.
            if (hasOperationalDept)
                return Array.Empty<string>();
        }

        if (!hasOperationalDept)
        {
            var allSetClaims = all.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return UserPlantResolution.GetStorePlantIds(user)
                .Where(p => allSetClaims.Contains(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return Array.Empty<string>();
    }

    public static async Task<SalesPlantScope> ResolveAsync(
        AppDbContext db,
        ClaimsPrincipal? user,
        string? requestedPlantId,
        IEnumerable<string> allPlantIdsFromDb,
        CancellationToken ct = default)
    {
        var all = NormalizePlantIds(allPlantIdsFromDb);
        var assigned = await LoadAssignedPlantIdsAsync(db, user, all, ct).ConfigureAwait(false);
        var isAdmin = UserPlantResolution.IsAdminDepartment(user);
        var hasOperationalDept = HasOperationalDepartment(user);
        return BuildScope(all, requestedPlantId, assigned, isAdmin, hasOperationalDept);
    }

    public static SalesPlantScope Resolve(
        ClaimsPrincipal? user,
        string? requestedPlantId,
        IEnumerable<string> allPlantIdsFromDb)
    {
        var all = NormalizePlantIds(allPlantIdsFromDb);
        var assigned = UserPlantResolution.GetStorePlantIds(user)
            .Where(p => all.Contains(p, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return BuildScope(all, requestedPlantId, assigned,
            UserPlantResolution.IsAdminDepartment(user),
            HasOperationalDepartment(user));
    }

    private static SalesPlantScope BuildScope(
        IReadOnlyList<string> all,
        string? requestedPlantId,
        IReadOnlyList<string> assigned,
        bool isAdminDepartment,
        bool hasOperationalDepartment)
    {
        var requested = (requestedPlantId ?? "").Trim();

        // Pure Admin (no Store/Sales/Production dept) with no plant rows → optional filter across all plants.
        // Users with Store, Sales, or Production MUST always be scoped to assigned plants (even if they also have Admin).
        if (isAdminDepartment && assigned.Count == 0 && !hasOperationalDepartment)
        {
            string? effective = null;
            if (requested.Length > 0 && all.Contains(requested, StringComparer.OrdinalIgnoreCase))
                effective = all.First(a => string.Equals(a, requested, StringComparison.OrdinalIgnoreCase));
            return new SalesPlantScope(all, effective, false, false, true);
        }

        if (assigned.Count == 0)
            return new SalesPlantScope(Array.Empty<string>(), null, false, true, false);

        if (assigned.Count == 1)
        {
            var only = assigned[0];
            return new SalesPlantScope(assigned, only, true, false, false);
        }

        if (requested.Length > 0
            && assigned.Contains(requested, StringComparer.OrdinalIgnoreCase))
        {
            var pick = assigned.First(a => string.Equals(a, requested, StringComparison.OrdinalIgnoreCase));
            return new SalesPlantScope(assigned, pick, false, false, false);
        }

        return new SalesPlantScope(assigned, null, false, false, false);
    }

    public static List<PlantsSample> FilterPlantsList(IEnumerable<PlantsSample> allPlants, SalesPlantScope scope)
    {
        var list = allPlants.OrderBy(p => p.PlantName).ToList();
        if (scope.IsAdminAllPlants)
            return list;
        if (scope.MissingAssignment)
            return new List<PlantsSample>();
        var allowed = scope.AllowedPlantIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return list.Where(p => allowed.Contains(p.PlantID)).ToList();
    }

    public static IQueryable<T> ApplyListingPlantFilter<T>(
        IQueryable<T> query,
        SalesPlantScope scope,
        Expression<Func<T, string?>> plantIdSelector)
    {
        if (scope.MissingAssignment)
            return query.Where(_ => false);

        if (scope.IsAdminAllPlants)
        {
            if (string.IsNullOrWhiteSpace(scope.EffectiveListPlantId))
                return query;
            return query.Where(BuildEquals(plantIdSelector, scope.EffectiveListPlantId!));
        }

        if (scope.IsSinglePlantLocked || !string.IsNullOrWhiteSpace(scope.EffectiveListPlantId))
        {
            var plant = scope.EffectiveListPlantId ?? scope.AllowedPlantIds[0];
            return query.Where(BuildEquals(plantIdSelector, plant));
        }

        return query.Where(BuildInList(plantIdSelector, scope.AllowedPlantIds));
    }

    /// <summary>Filter production orders where any line plant matches the resolved scope (orders have no header plant).</summary>
    public static IQueryable<ProductionOrder> ApplyProductionOrderPlantFilter(
        IQueryable<ProductionOrder> query,
        SalesPlantScope scope)
    {
        if (scope.MissingAssignment)
            return query.Where(_ => false);

        if (scope.IsAdminAllPlants)
        {
            if (string.IsNullOrWhiteSpace(scope.EffectiveListPlantId))
                return query;
            var adminPlant = scope.EffectiveListPlantId!;
            return query.Where(po => po.Lines.Any(l => l.PlantId == adminPlant));
        }

        if (scope.IsSinglePlantLocked || !string.IsNullOrWhiteSpace(scope.EffectiveListPlantId))
        {
            var plant = scope.EffectiveListPlantId ?? scope.AllowedPlantIds[0];
            return query.Where(po => po.Lines.Any(l => l.PlantId == plant));
        }

        var allowed = scope.AllowedPlantIds;
        return query.Where(po => po.Lines.Any(l => l.PlantId != null && allowed.Contains(l.PlantId)));
    }

    public static bool IsProductionOrderReadable(SalesPlantScope scope, IEnumerable<string?> linePlantIds)
    {
        if (scope.IsAdminAllPlants)
            return true;
        if (scope.MissingAssignment)
            return false;
        var plants = linePlantIds
            .Select(p => (p ?? "").Trim())
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (plants.Count == 0)
            return false;
        return plants.Any(p => scope.AllowedPlantIds.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    public static string? EnsurePlantAllowed(SalesPlantScope scope, string? plantId)
    {
        if (scope.MissingAssignment)
            return "Your user has no plant assigned. An administrator must assign at least one plant for your Store, Sales, or Production department.";

        var key = (plantId ?? "").Trim();
        if (scope.IsSinglePlantLocked)
            return null;

        if (scope.IsAdminAllPlants)
        {
            if (key.Length == 0)
                return "Plant is required.";
            if (!scope.AllowedPlantIds.Contains(key, StringComparer.OrdinalIgnoreCase))
                return "Invalid plant.";
            return null;
        }

        if (key.Length == 0)
            return "Plant is required.";
        if (!scope.AllowedPlantIds.Contains(key, StringComparer.OrdinalIgnoreCase))
            return "You are not allowed to use the selected plant.";
        return null;
    }

    public static string ResolveWritePlantId(SalesPlantScope scope, string? plantId)
    {
        if (scope.IsSinglePlantLocked && scope.EffectiveListPlantId != null)
            return scope.EffectiveListPlantId;
        return (plantId ?? "").Trim();
    }

    public static bool IsPlantReadable(SalesPlantScope scope, string? documentPlantId)
    {
        if (scope.IsAdminAllPlants)
            return true;
        if (scope.MissingAssignment)
            return false;
        var key = (documentPlantId ?? "").Trim();
        if (key.Length == 0)
            return false;
        return scope.AllowedPlantIds.Contains(key, StringComparer.OrdinalIgnoreCase);
    }

    public static void SetViewBag(Controller controller, SalesPlantScope scope, IEnumerable<PlantsSample> allPlants)
    {
        controller.ViewBag.SalesPlantScope = scope;
        var filtered = FilterPlantsList(allPlants, scope);
        controller.ViewBag.FilterPlants = filtered;
        controller.ViewBag.Plants = filtered;
        controller.ViewBag.MissingPlantAssignment = scope.MissingAssignment;
        controller.ViewBag.IsSinglePlantLocked = scope.IsSinglePlantLocked;
        controller.ViewBag.LockedPlantId = scope.EffectiveListPlantId
            ?? (scope.IsSinglePlantLocked ? scope.AllowedPlantIds.FirstOrDefault() : null);
    }

    private static List<string> NormalizePlantIds(IEnumerable<string> allPlantIdsFromDb) =>
        allPlantIdsFromDb
            .Select(id => (id ?? "").Trim())
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IEnumerable<string> ParsePlantCsv(string csv) =>
        (csv ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0);

    private static Expression<Func<T, bool>> BuildEquals<T>(
        Expression<Func<T, string?>> selector,
        string plantId)
    {
        var param = selector.Parameters[0];
        var notNull = Expression.NotEqual(
            selector.Body,
            Expression.Constant(null, typeof(string)));
        var equals = Expression.Equal(
            selector.Body,
            Expression.Constant(plantId, typeof(string)));
        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(notNull, equals),
            param);
    }

    /// <summary>OR-chain plant match — translates reliably in EF Core (unlike some Contains-on-constant patterns).</summary>
    private static Expression<Func<T, bool>> BuildInList<T>(
        Expression<Func<T, string?>> selector,
        IReadOnlyList<string> allowed)
    {
        var param = selector.Parameters[0];
        var notNull = Expression.NotEqual(
            selector.Body,
            Expression.Constant(null, typeof(string)));

        Expression? orBody = null;
        foreach (var plantId in allowed.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var eq = Expression.Equal(
                selector.Body,
                Expression.Constant(plantId, typeof(string)));
            orBody = orBody == null ? eq : Expression.OrElse(orBody, eq);
        }

        if (orBody == null)
            return _ => false;

        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(notNull, orBody),
            param);
    }
}
