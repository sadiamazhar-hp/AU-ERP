using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "AdminDepartment")]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public UsersController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var page = new UsersIndexPageModel
        {
            Users = await BuildUserListAsync().ConfigureAwait(false),
            CreateForm = new CreateUserViewModel(),
            OpenCreateModal = false
        };

        ViewBag.Departments = await _db.Departments.AsNoTracking()
            .OrderBy(d => d.Code)
            .ToListAsync()
            .ConfigureAwait(false);

        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        ViewBag.Departments = await _db.Departments.AsNoTracking()
            .OrderBy(d => d.Code)
            .ToListAsync()
            .ConfigureAwait(false);

        if (model.DepartmentIds == null || model.DepartmentIds.Length == 0)
            ModelState.AddModelError(nameof(model.DepartmentIds), "Select at least one department.");

        var validDeptIds = await _db.Departments.Select(d => d.Id).ToListAsync().ConfigureAwait(false);
        if (model.DepartmentIds != null && model.DepartmentIds.Any(id => !validDeptIds.Contains(id)))
            ModelState.AddModelError(nameof(model.DepartmentIds), "One or more departments are invalid.");

        if (!ModelState.IsValid)
        {
            return View("Index", new UsersIndexPageModel
            {
                Users = await BuildUserListAsync().ConfigureAwait(false),
                CreateForm = model,
                OpenCreateModal = true
            });
        }

        var existing = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
            return View("Index", new UsersIndexPageModel
            {
                Users = await BuildUserListAsync().ConfigureAwait(false),
                CreateForm = model,
                OpenCreateModal = true
            });
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            PasswordSetupCompleted = false
        };

        var createResult = await _userManager.CreateAsync(user).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            foreach (var err in createResult.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return View("Index", new UsersIndexPageModel
            {
                Users = await BuildUserListAsync().ConfigureAwait(false),
                CreateForm = model,
                OpenCreateModal = true
            });
        }

        foreach (var deptId in (model.DepartmentIds ?? Array.Empty<int>()).Distinct())
        {
            _db.ApplicationUserDepartments.Add(new ApplicationUserDepartment
            {
                UserId = user.Id,
                DepartmentId = deptId
            });
        }

        await _db.SaveChangesAsync().ConfigureAwait(false);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        await PasswordEmail.SendInvitationAsync(_emailService, _configuration, user, token).ConfigureAwait(false);

        TempData["UserMessage"] = "User added. An invitation email was sent.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<UserListItemViewModel>> BuildUserListAsync()
    {
        var users = await _db.Users.AsNoTracking()
            .Include(u => u.UserDepartments)
            .ThenInclude(ud => ud.Department)
            .OrderBy(u => u.Email)
            .ToListAsync()
            .ConfigureAwait(false);

        return users.Select(u => new UserListItemViewModel
        {
            Id = u.Id,
            Email = u.Email ?? "",
            FirstName = u.FirstName ?? "",
            LastName = u.LastName ?? "",
            PasswordSetupCompleted = u.PasswordSetupCompleted,
            Departments = u.UserDepartments.Select(ud => ud.Department.Code).OrderBy(c => c).ToList()
        }).ToList();
    }
}
