using System.Security.Claims;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AU_ERP.Controllers;

public class CompanyInfoController : Controller
{
    private readonly CompanyInfoService _companyInfo;

    public CompanyInfoController(CompanyInfoService companyInfo) => _companyInfo = companyInfo;

    private bool IsAdminDepartment =>
        User.HasClaim(AuClaimTypes.Department, "Admin");

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewData["Title"] = "Company info";
        var vm = await _companyInfo.GetEditVmAsync(IsAdminDepartment, ct).ConfigureAwait(false);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminDepartment")]
    public async Task<IActionResult> Save(
        [Bind(
            nameof(CompanyInfoEditVm.CompanyName),
            nameof(CompanyInfoEditVm.Address),
            nameof(CompanyInfoEditVm.City),
            nameof(CompanyInfoEditVm.Country),
            nameof(CompanyInfoEditVm.Language),
            nameof(CompanyInfoEditVm.PhoneNumber),
            nameof(CompanyInfoEditVm.Fax),
            nameof(CompanyInfoEditVm.Email),
            nameof(CompanyInfoEditVm.Website),
            nameof(CompanyInfoEditVm.TaxNumberNtn),
            nameof(CompanyInfoEditVm.GstOrTaxRate),
            nameof(CompanyInfoEditVm.BankCountry),
            nameof(CompanyInfoEditVm.BankKey),
            nameof(CompanyInfoEditVm.BankName),
            nameof(CompanyInfoEditVm.BankAccountNumber),
            nameof(CompanyInfoEditVm.BankAccountHolderName),
            nameof(CompanyInfoEditVm.Branch),
            nameof(CompanyInfoEditVm.BankAddress))]
        CompanyInfoEditVm model,
        CancellationToken ct = default)
    {
        ViewData["Title"] = "Company info";
        model.CanEdit = true;
        if (!ModelState.IsValid)
            return View("Index", model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var err = await _companyInfo.SaveAsync(model, userId, ct).ConfigureAwait(false);
        if (err != null)
        {
            ModelState.AddModelError(string.Empty, err);
            return View("Index", model);
        }

        TempData["CompanyInfoMessage"] = "Company information saved.";
        return RedirectToAction(nameof(Index));
    }
}
