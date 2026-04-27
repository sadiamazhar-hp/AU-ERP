using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool PasswordSetupCompleted { get; set; }
    public IReadOnlyList<string> Departments { get; set; } = Array.Empty<string>();
}

public class CreateUserViewModel
{
    [Required]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public int[] DepartmentIds { get; set; } = Array.Empty<int>();

    [Display(Name = "Store plant")]
    public string? StorePlantId { get; set; }
}

public class EditUserViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    public int[] DepartmentIds { get; set; } = Array.Empty<int>();

    [Display(Name = "Store plant")]
    public string? StorePlantId { get; set; }
}

public class UsersIndexPageModel
{
    public IReadOnlyList<UserListItemViewModel> Users { get; set; } = Array.Empty<UserListItemViewModel>();
    public CreateUserViewModel CreateForm { get; set; } = new();
    public bool OpenCreateModal { get; set; }
    public EditUserViewModel EditForm { get; set; } = new();
    public bool OpenEditModal { get; set; }
}
