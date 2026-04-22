using AU_ERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AU_ERP.Authorization
{
    public class CanManageUsersHandler : AuthorizationHandler<CanManageUsersRequirement>
    {
        private readonly AppDbContext _db;

        public CanManageUsersHandler(AppDbContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CanManageUsersRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return;

            var isAdminDept = await _db.ApplicationUserDepartments
                .AsNoTracking()
                .AnyAsync(ud => ud.UserId == userId && ud.Department.Code == "Admin")
                .ConfigureAwait(false);

            if (isAdminDept)
                context.Succeed(requirement);
        }
    }
}
