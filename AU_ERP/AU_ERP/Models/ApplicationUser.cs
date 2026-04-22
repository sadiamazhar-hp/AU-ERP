using Microsoft.AspNetCore.Identity;

namespace AU_ERP.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        /// <summary>False until the user completes the email invite or first-time password setup.</summary>
        public bool PasswordSetupCompleted { get; set; }

        public ICollection<ApplicationUserDepartment> UserDepartments { get; set; } = new List<ApplicationUserDepartment>();
    }
}
