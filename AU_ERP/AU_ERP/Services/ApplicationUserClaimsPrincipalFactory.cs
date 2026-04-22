using System.Security.Claims;
using AU_ERP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AU_ERP.Services
{
    public static class AuClaimTypes
    {
        public const string Department = "department";
    }

    /// <summary>Adds department claims for lightweight UI checks (navigation). Authorization still uses DB-backed policy.</summary>
    public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        private readonly AppDbContext _db;

        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> options,
            AppDbContext db)
            : base(userManager, roleManager, options)
        {
            _db = db;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user).ConfigureAwait(false);
            var codes = await _db.ApplicationUserDepartments
                .AsNoTracking()
                .Where(ud => ud.UserId == user.Id)
                .Select(ud => ud.Department.Code)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var code in codes)
                identity.AddClaim(new Claim(AuClaimTypes.Department, code));

            return identity;
        }
    }
}
