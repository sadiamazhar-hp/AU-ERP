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
        /// <summary>Value is <see cref="PlantsSample.PlantID"/> for Store or Sales department plant assignments.</summary>
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

            var plantClaims = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in deptRows)
            {
                identity.AddClaim(new Claim(AuClaimTypes.Department, row.Code));
                if ((string.Equals(row.Code, "Store", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(row.Code, "Sales", StringComparison.OrdinalIgnoreCase))
                    && !string.IsNullOrWhiteSpace(row.PlantID))
                {
                    foreach (var p in row.PlantID!
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0))
                    {
                        plantClaims.Add(p);
                    }
                }
            }

            foreach (var stale in identity.FindAll(AuClaimTypes.StorePlant).ToList())
                identity.RemoveClaim(stale);

            foreach (var p in plantClaims)
                identity.AddClaim(new Claim(AuClaimTypes.StorePlant, p));

            return identity;
        }
    }
}
