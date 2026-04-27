using System.Linq;
using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Controllers;

[Authorize(Policy = "AdminDepartment")]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public UsersController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? editId = null)
    {
        var users = await BuildUserListAsync().ConfigureAwait(false);
        var editForm = new EditUserViewModel();
        var openEdit = false;
        if (!string.IsNullOrEmpty(editId))
        {
            var toEdit = await _db.Users
                .AsNoTracking()
                .Include(u => u.UserDepartments)
                .FirstOrDefaultAsync(u => u.Id == editId)
                .ConfigureAwait(false);
            if (toEdit != null)
            {
                var storeDeptId = await _db.Departments.AsNoTracking()
                    .Where(d => d.Code == "Store")
                    .Select(d => d.Id)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
                string? storePlant = null;
                if (storeDeptId > 0)
                {
                    storePlant = await _db.ApplicationUserDepartments.AsNoTracking()
                        .Where(ud => ud.UserId == toEdit.Id && ud.DepartmentId == storeDeptId)
                        .Select(ud => ud.PlantID)
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);
                }

                editForm = new EditUserViewModel
                {
                    UserId = toEdit.Id,
                    FirstName = toEdit.FirstName?.Trim() ?? "",
                    LastName = toEdit.LastName?.Trim() ?? "",
                    Email = toEdit.Email ?? "",
                    DepartmentIds = toEdit.UserDepartments.Select(ud => ud.DepartmentId).ToArray(),
                    StorePlantId = storePlant
                };
                openEdit = true;
            }
        }

        var page = new UsersIndexPageModel
        {
            Users = users,
            CreateForm = new CreateUserViewModel(),
            OpenCreateModal = false,
            EditForm = editForm,
            OpenEditModal = openEdit
        };

        ViewBag.Departments = await _db.Departments.AsNoTracking()
            .OrderBy(d => d.Code)
            .ToListAsync()
            .ConfigureAwait(false);
        ViewBag.SeededAdminUserId = await GetSeededAdminUserIdAsync().ConfigureAwait(false);
        await FillUserPlantLookupsAsync().ConfigureAwait(false);

        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind(Prefix = "EditForm")] EditUserViewModel model)
    {
        const string pfx = "EditForm.";
        ViewBag.Departments = await _db.Departments.AsNoTracking()
            .OrderBy(d => d.Code)
            .ToListAsync()
            .ConfigureAwait(false);
        ViewBag.SeededAdminUserId = await GetSeededAdminUserIdAsync().ConfigureAwait(false);
        await FillUserPlantLookupsAsync().ConfigureAwait(false);

        if (model.DepartmentIds == null || model.DepartmentIds.Length == 0)
            ModelState.AddModelError(pfx + nameof(model.DepartmentIds), "Select at least one department.");

        var validDeptIds = await _db.Departments.Select(d => d.Id).ToListAsync().ConfigureAwait(false);
        if (model.DepartmentIds != null && model.DepartmentIds.Any(id => !validDeptIds.Contains(id)))
            ModelState.AddModelError(pfx + nameof(model.DepartmentIds), "One or more departments are invalid.");

        await ValidateStorePlantAssignmentAsync(ModelState, pfx, model.DepartmentIds, model.StorePlantId).ConfigureAwait(false);

        var user = await _userManager.FindByIdAsync(model.UserId).ConfigureAwait(false);
        if (user == null)
            ModelState.AddModelError(string.Empty, "User not found.");
        else
        {
            var adminEmail = (_configuration["AdminUser:Email"] ?? "").Trim();
            if (adminEmail.Length > 0
                && !string.IsNullOrEmpty(user.Email)
                && string.Equals(user.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
            {
                var adminDeptId = await _db.Departments.AsNoTracking()
                    .Where(d => d.Code == "Admin")
                    .Select(d => d.Id)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
                if (adminDeptId > 0)
                {
                    var ids = (model.DepartmentIds ?? Array.Empty<int>()).ToList();
                    if (!ids.Contains(adminDeptId)) ids.Add(adminDeptId);
                    model.DepartmentIds = ids.Distinct().ToArray();
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return View("Index", new UsersIndexPageModel
            {
                Users = await BuildUserListAsync().ConfigureAwait(false),
                CreateForm = new CreateUserViewModel(),
                OpenCreateModal = false,
                EditForm = model,
                OpenEditModal = true
            });
        }

        user!.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();

        var updateResult = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!updateResult.Succeeded)
        {
            foreach (var err in updateResult.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return View("Index", new UsersIndexPageModel
            {
                Users = await BuildUserListAsync().ConfigureAwait(false),
                CreateForm = new CreateUserViewModel(),
                OpenCreateModal = false,
                EditForm = model,
                OpenEditModal = true
            });
        }

        var existing = await _db.ApplicationUserDepartments
            .Where(ud => ud.UserId == user.Id)
            .ToListAsync()
            .ConfigureAwait(false);
        _db.ApplicationUserDepartments.RemoveRange(existing);
        var storeDeptId = await _db.Departments.AsNoTracking()
            .Where(d => d.Code == "Store").Select(d => d.Id).FirstOrDefaultAsync().ConfigureAwait(false);
        foreach (var deptId in model.DepartmentIds!.Distinct())
        {
            string? plantIdLink = null;
            if (storeDeptId > 0 && deptId == storeDeptId)
            {
                var p = (model.StorePlantId ?? "").Trim();
                plantIdLink = p.Length > 0 ? p : null;
            }

            _db.ApplicationUserDepartments.Add(new ApplicationUserDepartment
            {
                UserId = user.Id,
                DepartmentId = deptId,
                PlantID = plantIdLink
            });
        }

        await _db.SaveChangesAsync().ConfigureAwait(false);

        if (string.Equals(_userManager.GetUserId(User), user.Id, StringComparison.Ordinal))
        {
            var refreshed = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (refreshed != null)
                await _signInManager.RefreshSignInAsync(refreshed).ConfigureAwait(false);
        }

        TempData["UserMessage"] = "User updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateUserViewModel model)
    {
        const string pfx = "CreateForm.";
        ViewBag.Departments = await _db.Departments.AsNoTracking()
            .OrderBy(d => d.Code)
            .ToListAsync()
            .ConfigureAwait(false);
        await FillUserPlantLookupsAsync().ConfigureAwait(false);

        if (model.DepartmentIds == null || model.DepartmentIds.Length == 0)
            ModelState.AddModelError(pfx + nameof(model.DepartmentIds), "Select at least one department.");

        var validDeptIds = await _db.Departments.Select(d => d.Id).ToListAsync().ConfigureAwait(false);
        if (model.DepartmentIds != null && model.DepartmentIds.Any(id => !validDeptIds.Contains(id)))
            ModelState.AddModelError(pfx + nameof(model.DepartmentIds), "One or more departments are invalid.");

        await ValidateStorePlantAssignmentAsync(ModelState, pfx, model.DepartmentIds, model.StorePlantId).ConfigureAwait(false);

        if (!ModelState.IsValid)
        {
            return View("Index", new UsersIndexPageModel
            {
                Users = await BuildUserListAsync().ConfigureAwait(false),
                CreateForm = model,
                OpenCreateModal = true,
                EditForm = new EditUserViewModel(),
                OpenEditModal = false
            });
        }

        var existing = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
        if (existing != null)
        {
            ModelState.AddModelError("CreateForm.Email", "A user with this email already exists.");
            return View("Index", new UsersIndexPageModel
            {
                Users = await BuildUserListAsync().ConfigureAwait(false),
                CreateForm = model,
                OpenCreateModal = true,
                EditForm = new EditUserViewModel(),
                OpenEditModal = false
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
                OpenCreateModal = true,
                EditForm = new EditUserViewModel(),
                OpenEditModal = false
            });
        }

        var storeDeptIdCreate = await _db.Departments.AsNoTracking()
            .Where(d => d.Code == "Store").Select(d => d.Id).FirstOrDefaultAsync().ConfigureAwait(false);
        foreach (var deptId in (model.DepartmentIds ?? Array.Empty<int>()).Distinct())
        {
            string? plantIdLink = null;
            if (storeDeptIdCreate > 0 && deptId == storeDeptIdCreate)
            {
                var p = (model.StorePlantId ?? "").Trim();
                plantIdLink = p.Length > 0 ? p : null;
            }

            _db.ApplicationUserDepartments.Add(new ApplicationUserDepartment
            {
                UserId = user.Id,
                DepartmentId = deptId,
                PlantID = plantIdLink
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

    private async Task<string?> GetSeededAdminUserIdAsync()
    {
        var email = (_configuration["AdminUser:Email"] ?? "").Trim();
        if (email.Length == 0) return null;
        var u = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        return u?.Id;
    }

    private async Task FillUserPlantLookupsAsync()
    {
        ViewBag.StoreDepartmentId = await _db.Departments.AsNoTracking()
            .Where(d => d.Code == "Store")
            .Select(d => d.Id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
        ViewBag.Plants = await _db.PlantsSamples.AsNoTracking()
            .OrderBy(p => p.PlantName)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    private async Task ValidateStorePlantAssignmentAsync(
        ModelStateDictionary modelState,
        string fieldPrefix,
        int[]? departmentIds,
        string? storePlantId)
    {
        var storeDeptId = await _db.Departments.AsNoTracking()
            .Where(d => d.Code == "Store")
            .Select(d => d.Id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
        if (storeDeptId <= 0)
            return;

        var hasStore = departmentIds?.Contains(storeDeptId) ?? false;
        var trimmed = (storePlantId ?? "").Trim();

        if (hasStore)
        {
            if (trimmed.Length == 0)
            {
                modelState.AddModelError($"{fieldPrefix}StorePlantId",
                    "Select a plant when the Store department is assigned.");
                return;
            }

            var okPlant = await _db.PlantsSamples.AsNoTracking()
                .AnyAsync(p => p.PlantID == trimmed)
                .ConfigureAwait(false);
            if (!okPlant)
                modelState.AddModelError($"{fieldPrefix}StorePlantId", "Selected plant is invalid.");
        }
        else if (trimmed.Length > 0)
        {
            modelState.AddModelError($"{fieldPrefix}StorePlantId",
                "Plant is only applicable when Store department is selected.");
        }
    }
}
