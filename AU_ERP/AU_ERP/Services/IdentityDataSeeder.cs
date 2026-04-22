using AU_ERP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AU_ERP.Services;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AppDbContext>();
        var log = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentityDataSeeder));

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var emailService = provider.GetRequiredService<IEmailService>();

        var adminEmail = configuration["AdminUser:Email"] ?? "admin@localhost";
        var adminUserName = configuration["AdminUser:UserName"] ?? "admin";

        var existing = await userManager.FindByEmailAsync(adminEmail).ConfigureAwait(false);
        if (existing != null)
            return;

        var admin = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User",
            PasswordSetupCompleted = false
        };

        var createResult = await userManager.CreateAsync(admin).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed admin user: {errors}");
        }

        var adminDeptId = await db.Departments.Where(d => d.Code == "Admin").Select(d => d.Id).FirstAsync().ConfigureAwait(false);
        db.ApplicationUserDepartments.Add(new ApplicationUserDepartment
        {
            UserId = admin.Id,
            DepartmentId = adminDeptId
        });
        await db.SaveChangesAsync().ConfigureAwait(false);

        var token = await userManager.GeneratePasswordResetTokenAsync(admin).ConfigureAwait(false);
        var setupLink = AccountLinkBuilder.PasswordSetLink(configuration, admin.Id, token);

        if (string.IsNullOrWhiteSpace(configuration["Email:SmtpHost"]))
        {
            log.LogWarning(
                "Email is NOT sent: Email:SmtpHost is empty in configuration. " +
                "Use the link below to set the admin password (this link is shown only because SMTP is not configured). Setup link: {SetupLink}",
                setupLink);
        }

        await PasswordEmail.SendInvitationAsync(emailService, configuration, admin, token).ConfigureAwait(false);
    }
}
