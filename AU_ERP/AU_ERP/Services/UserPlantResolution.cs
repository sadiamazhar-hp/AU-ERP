using System.Security.Claims;

namespace AU_ERP.Services;

public static class UserPlantResolution
{
    /// <summary>Users in the Admin department can use all plants for delivery challan flows.</summary>
    public static bool IsAdminDepartment(ClaimsPrincipal? user) =>
        user?.HasClaim(AuClaimTypes.Department, "Admin") == true;

    /// <summary><see cref="PlantsSample.PlantID"/> for Emporium (walk-in retail).</summary>
    public const string EmporiumPlantId = "Emp101";

    /// <summary>True when the user has Store access to Emporium plant (store_plant claim).</summary>
    public static bool HasEmporiumStorePlant(ClaimsPrincipal? user) =>
        GetStorePlantIds(user).Contains(EmporiumPlantId, StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the plant (<see cref="PlantsSample"/>) assigned to this user via the Store department link, if any.</summary>
    public static string? TryGetStorePlantId(ClaimsPrincipal? user)
    {
        return GetStorePlantIds(user).FirstOrDefault();
    }

    /// <summary>Returns all distinct store plants assigned to this user.</summary>
    public static IReadOnlyList<string> GetStorePlantIds(ClaimsPrincipal? user)
    {
        if (user == null)
            return Array.Empty<string>();

        return user.FindAll(AuClaimTypes.StorePlant)
            .Select(c => (c.Value ?? "").Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool HasStorePlantAccess(ClaimsPrincipal? user, string? plantId)
    {
        var key = (plantId ?? "").Trim();
        if (key.Length == 0)
            return false;
        return GetStorePlantIds(user).Contains(key, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Plant ids allowed on delivery challan for this user: assigned store plants when present, otherwise all plants from master data.
    /// </summary>
    public static IReadOnlyList<string> GetDeliveryChallanPlantIdFilter(ClaimsPrincipal? user, IEnumerable<string> allPlantIdsFromDb)
    {
        var all = allPlantIdsFromDb
            .Select(id => (id ?? "").Trim())
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (IsAdminDepartment(user))
            return all;
        var assigned = GetStorePlantIds(user);
        if (assigned.Count == 0)
            return all;

        var allSet = all.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filtered = assigned.Where(a => allSet.Contains(a)).ToList();
        return filtered.Count > 0 ? filtered : all;
    }

    /// <summary>True when the user has exactly one assigned store plant (DC plant field should be read-only).</summary>
    public static bool IsDeliveryChallanPlantSingleLocked(ClaimsPrincipal? user) =>
        GetStorePlantIds(user).Count == 1;
}
