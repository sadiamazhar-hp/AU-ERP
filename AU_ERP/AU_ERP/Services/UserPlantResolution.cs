using System.Security.Claims;

namespace AU_ERP.Services;

public static class UserPlantResolution
{
    /// <summary>Returns the plant (<see cref="PlantsSample"/>) assigned to this user via the Store department link, if any.</summary>
    public static string? TryGetStorePlantId(ClaimsPrincipal? user)
    {
        if (user == null)
            return null;
        var v = user.FindFirst(AuClaimTypes.StorePlant)?.Value?.Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }
}
