using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AU_ERP.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("GetMaterialList", "Material");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (!user.PasswordSetupCompleted || string.IsNullOrEmpty(user.PasswordHash))
        {
            ModelState.AddModelError(
                string.Empty,
                "You have not set your password yet. Use the link in your invitation email, or use Forgot password to receive a new link.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false)
            .ConfigureAwait(false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("GetMaterialList", "Material");
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync().ConfigureAwait(false);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
            if (!user.PasswordSetupCompleted || string.IsNullOrEmpty(user.PasswordHash))
                await PasswordEmail.SendInvitationAsync(_emailService, _configuration, user, token).ConfigureAwait(false);
            else
                await PasswordEmail.SendForgotPasswordAsync(_emailService, _configuration, user, token).ConfigureAwait(false);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> SetPassword()
    {
        var userId = Request.Query["userId"].FirstOrDefault();
        var code = Request.Query["code"].FirstOrDefault();
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            return BadRequest();

        var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
            return NotFound();

        var vm = new SetPasswordViewModel
        {
            UserId = userId,
            Code = code
        };
        ViewData["Title"] = user.PasswordSetupCompleted ? "Reset password" : "Set password";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        ViewData["Title"] = "Set password";
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.UserId).ConfigureAwait(false);
        if (user == null)
            return NotFound();

        ViewData["Title"] = user.PasswordSetupCompleted ? "Reset password" : "Set password";

        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.NewPassword).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return View(model);
        }

        user.PasswordSetupCompleted = true;
        await _userManager.UpdateAsync(user).ConfigureAwait(false);

        TempData["AccountMessage"] = "Your password has been saved. You can sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
