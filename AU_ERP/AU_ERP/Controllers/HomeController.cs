using System.Diagnostics;
using AU_ERP.Models;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AU_ERP.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>Entry screen (default route): brief animation, then unauthenticated users continue to login.</summary>
        public IActionResult Splash()
        {
            if (User.Identity?.IsAuthenticated == true)
                return Redirect(DepartmentLanding.GetPath(User));
            return View();
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return Redirect(DepartmentLanding.GetPath(User));
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
