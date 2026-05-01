using System.Security.Claims;

namespace AU_ERP.Services;

public static class UserPlantResolution
{
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
}
