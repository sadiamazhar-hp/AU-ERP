using System.Security.Claims;

namespace AU_ERP.Services;

/// <summary>First app URL after sign-in, based on department claims.</summary>
public static class DepartmentLanding
{
    public static string GetPath(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return "/Account/Login";
        if (user.HasClaim(AuClaimTypes.Department, "Admin"))
            return "/Material/GetMaterialList";
        if (user.HasClaim(AuClaimTypes.Department, "Production"))
            return "/MRP/Index";
        return "/Home/Index";
    }
}
