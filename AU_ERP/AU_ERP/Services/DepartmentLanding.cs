using System.Security.Claims;

namespace AU_ERP.Services;

/// <summary>First app URL after sign-in, based on department claims.</summary>
public static class DepartmentLanding
{
    public static string GetPath(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return "/Account/Login";
        return "/Dashboard/Index";
    }
}
