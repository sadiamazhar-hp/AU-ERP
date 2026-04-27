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
        /// <summary>Value is <see cref="PlantsSample.PlantID"/> for the user’s Store department assignment.</summary>
        public const string StorePlant = "store_plant";
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
            var deptRows = await _db.ApplicationUserDepartments
                .AsNoTracking()
                .Where(ud => ud.UserId == user.Id)
                .Select(ud => new { ud.Department!.Code, ud.PlantID })
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var row in deptRows)
            {
                identity.AddClaim(new Claim(AuClaimTypes.Department, row.Code));
                if (string.Equals(row.Code, "Store", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(row.PlantID))
                    identity.AddClaim(new Claim(AuClaimTypes.StorePlant, row.PlantID!.Trim()));
            }

            return identity;
        }
    }
}
