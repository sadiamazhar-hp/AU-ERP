using AU_ERP.Models;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(10);
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminDepartment", policy =>
        policy.RequireClaim(AuClaimTypes.Department, "Admin"));
    options.AddPolicy("ProductionDepartment", policy =>
        policy.RequireClaim(AuClaimTypes.Department, "Production"));
    // Quotation, sales order, delivery challan: users with Sales department claim only.
    options.AddPolicy("SalesDepartment", policy =>
        policy.RequireClaim(AuClaimTypes.Department, "Sales"));
    // Stock overview: users with Store department claim (seed data).
    options.AddPolicy("StoreDepartment", policy =>
        policy.RequireClaim(AuClaimTypes.Department, "Store"));
});

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<DashboardDataService>();

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Splash}/{id?}");

await IdentityDataSeeder.SeedAsync(app.Services, app.Configuration).ConfigureAwait(false);
await ConfigurationLookupSeeder.SeedAsync(app.Services).ConfigureAwait(false);

await app.RunAsync().ConfigureAwait(false);
