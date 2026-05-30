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
                var storePlants = await LoadAssignedPlantIdsForUserAsync(toEdit.Id).ConfigureAwait(false);

                editForm = new EditUserViewModel
                {
                    UserId = toEdit.Id,
                    FirstName = toEdit.FirstName?.Trim() ?? "",
                    LastName = toEdit.LastName?.Trim() ?? "",
                    Email = toEdit.Email ?? "",
                    DepartmentIds = toEdit.UserDepartments.Select(ud => ud.DepartmentId).ToArray(),
                    StorePlantIds = storePlants
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

        await ValidateStorePlantAssignmentAsync(ModelState, pfx, model.DepartmentIds, model.StorePlantIds).ConfigureAwait(false);

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
        var (storeDeptId, salesDeptId) = await GetStoreAndSalesDepartmentIdsAsync().ConfigureAwait(false);
        var plantCsv = JoinStorePlantIds(model.StorePlantIds);
        var hasStore = storeDeptId > 0 && (model.DepartmentIds ?? Array.Empty<int>()).Contains(storeDeptId);
        var hasSales = salesDeptId > 0 && (model.DepartmentIds ?? Array.Empty<int>()).Contains(salesDeptId);
        var needsPlants = hasStore || hasSales;
        foreach (var deptId in model.DepartmentIds!.Distinct())
        {
            string? rowPlantId = null;
            if (needsPlants && (deptId == storeDeptId || deptId == salesDeptId))
                rowPlantId = plantCsv;
            _db.ApplicationUserDepartments.Add(new ApplicationUserDepartment
            {
                UserId = user.Id,
                DepartmentId = deptId,
                PlantID = rowPlantId
            });
        }

        await _db.SaveChangesAsync().ConfigureAwait(false);
        await SyncStorePlantClaimsAsync(
            user,
            needsPlants ? model.StorePlantIds : Array.Empty<string>())
            .ConfigureAwait(false);

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

        await ValidateStorePlantAssignmentAsync(ModelState, pfx, model.DepartmentIds, model.StorePlantIds).ConfigureAwait(false);

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

        var (storeDeptIdCreate, salesDeptIdCreate) = await GetStoreAndSalesDepartmentIdsAsync().ConfigureAwait(false);
        var plantCsvCreate = JoinStorePlantIds(model.StorePlantIds);
        var hasStoreCreate = storeDeptIdCreate > 0 && (model.DepartmentIds ?? Array.Empty<int>()).Contains(storeDeptIdCreate);
        var hasSalesCreate = salesDeptIdCreate > 0 && (model.DepartmentIds ?? Array.Empty<int>()).Contains(salesDeptIdCreate);
        var needsPlantsCreate = hasStoreCreate || hasSalesCreate;
        foreach (var deptId in (model.DepartmentIds ?? Array.Empty<int>()).Distinct())
        {
            string? rowPlantId = null;
            if (needsPlantsCreate && (deptId == storeDeptIdCreate || deptId == salesDeptIdCreate))
                rowPlantId = plantCsvCreate;
            _db.ApplicationUserDepartments.Add(new ApplicationUserDepartment
            {
                UserId = user.Id,
                DepartmentId = deptId,
                PlantID = rowPlantId
            });
        }

        await _db.SaveChangesAsync().ConfigureAwait(false);
        await SyncStorePlantClaimsAsync(
            user,
            needsPlantsCreate ? model.StorePlantIds : Array.Empty<string>())
            .ConfigureAwait(false);

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
        var (storeDeptId, salesDeptId) = await GetStoreAndSalesDepartmentIdsAsync().ConfigureAwait(false);
        ViewBag.StoreDepartmentId = storeDeptId;
        ViewBag.SalesDepartmentId = salesDeptId;
        ViewBag.Plants = await _db.PlantsSamples.AsNoTracking()
            .OrderBy(p => p.PlantName)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    private async Task<(int storeDeptId, int salesDeptId)> GetStoreAndSalesDepartmentIdsAsync()
    {
        var rows = await _db.Departments.AsNoTracking()
            .Where(d => d.Code == "Store" || d.Code == "Sales")
            .Select(d => new { d.Code, d.Id })
            .ToListAsync()
            .ConfigureAwait(false);
        var storeDeptId = rows.FirstOrDefault(r => r.Code == "Store")?.Id ?? 0;
        var salesDeptId = rows.FirstOrDefault(r => r.Code == "Sales")?.Id ?? 0;
        return (storeDeptId, salesDeptId);
    }

    private async Task<string[]> LoadAssignedPlantIdsForUserAsync(string userId)
    {
        var (storeDeptId, salesDeptId) = await GetStoreAndSalesDepartmentIdsAsync().ConfigureAwait(false);
        var deptIds = new[] { storeDeptId, salesDeptId }.Where(id => id > 0).ToList();
        if (deptIds.Count == 0)
            return Array.Empty<string>();

        var csvs = await _db.ApplicationUserDepartments.AsNoTracking()
            .Where(ud => ud.UserId == userId && deptIds.Contains(ud.DepartmentId))
            .Select(ud => ud.PlantID)
            .ToListAsync()
            .ConfigureAwait(false);

        var fromDb = csvs
            .SelectMany(c => ParseStorePlantIds(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (fromDb.Length > 0)
            return fromDb;

        var userForClaims = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (userForClaims == null)
            return Array.Empty<string>();

        var claims = await _userManager.GetClaimsAsync(userForClaims).ConfigureAwait(false);
        return claims
            .Where(c => string.Equals(c.Type, AuClaimTypes.StorePlant, StringComparison.Ordinal))
            .Select(c => (c.Value ?? "").Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task ValidateStorePlantAssignmentAsync(
        ModelStateDictionary modelState,
        string fieldPrefix,
        int[]? departmentIds,
        string[]? storePlantIds)
    {
        var (storeDeptId, salesDeptId) = await GetStoreAndSalesDepartmentIdsAsync().ConfigureAwait(false);

        var hasStore = storeDeptId > 0 && (departmentIds?.Contains(storeDeptId) ?? false);
        var hasSales = salesDeptId > 0 && (departmentIds?.Contains(salesDeptId) ?? false);
        var needsPlants = hasStore || hasSales;
        var ids = (storePlantIds ?? Array.Empty<string>())
            .Select(p => (p ?? "").Trim())
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (needsPlants)
        {
            if (ids.Length == 0)
            {
                modelState.AddModelError($"{fieldPrefix}StorePlantIds",
                    "Select at least one assigned plant when the Store or Sales department is assigned.");
                return;
            }

            var validCount = await _db.PlantsSamples.AsNoTracking()
                .Where(p => ids.Contains(p.PlantID))
                .CountAsync()
                .ConfigureAwait(false);
            if (validCount != ids.Length)
                modelState.AddModelError($"{fieldPrefix}StorePlantIds", "One or more selected plants are invalid.");
        }
        else if (ids.Length > 0)
        {
            modelState.AddModelError($"{fieldPrefix}StorePlantIds",
                "Assigned plants are only applicable when Store or Sales department is selected.");
        }
    }

    private static string[] ParseStorePlantIds(string? csv)
    {
        return (csv ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? JoinStorePlantIds(string[]? ids)
    {
        var values = (ids ?? Array.Empty<string>())
            .Select(x => (x ?? "").Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return values.Length == 0 ? null : string.Join(",", values);
    }

    private async Task SyncStorePlantClaimsAsync(ApplicationUser user, string[]? storePlantIds)
    {
        var claims = await _userManager.GetClaimsAsync(user).ConfigureAwait(false);
        var existing = claims
            .Where(c => string.Equals(c.Type, AuClaimTypes.StorePlant, StringComparison.Ordinal))
            .ToList();
        if (existing.Count > 0)
        {
            var rm = await _userManager.RemoveClaimsAsync(user, existing).ConfigureAwait(false);
            if (!rm.Succeeded)
                throw new InvalidOperationException("Failed to clear existing store plant claims.");
        }

        var distinct = (storePlantIds ?? Array.Empty<string>())
            .Select(x => (x ?? "").Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (distinct.Length == 0)
            return;

        var toAdd = distinct
            .Select(p => new System.Security.Claims.Claim(AuClaimTypes.StorePlant, p))
            .ToList();
        var add = await _userManager.AddClaimsAsync(user, toAdd).ConfigureAwait(false);
        if (!add.Succeeded)
            throw new InvalidOperationException("Failed to save store plant claims.");
    }
}
