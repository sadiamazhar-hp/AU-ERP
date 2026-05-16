using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Mvc;

namespace AU_ERP.Controllers;

public class DashboardController : Controller
{
    private readonly DashboardDataService _dashboardData;

    public DashboardController(DashboardDataService dashboardData)
    {
        _dashboardData = dashboardData;
    }

    public async Task<IActionResult> Index(string? period, CancellationToken ct = default)
    {
        ViewData["Title"] = "Dashboard";
        DashboardPageVm vm = await _dashboardData.BuildAsync(User, period, ct).ConfigureAwait(false);
        return View(vm);
    }
}
