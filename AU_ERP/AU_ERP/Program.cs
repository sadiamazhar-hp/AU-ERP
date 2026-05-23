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
    options.AddPolicy("SalesOrAdminDepartment", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(AuClaimTypes.Department, "Sales")
            || ctx.User.HasClaim(AuClaimTypes.Department, "Admin")));
    // Stock overview: users with Store department claim (seed data).
    options.AddPolicy("StoreDepartment", policy =>
        policy.RequireClaim(AuClaimTypes.Department, "Store"));
    options.AddPolicy("InventoryGoodsIssue", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(AuClaimTypes.Department, "Store")
            || ctx.User.HasClaim(AuClaimTypes.Department, "Production")
            || ctx.User.HasClaim(AuClaimTypes.Department, "Sales")
            || ctx.User.HasClaim(AuClaimTypes.Department, "Admin")));
});

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<DashboardDataService>();
builder.Services.AddScoped<GoodsReceiptPostingService>();
builder.Services.AddScoped<GoodReceiptPdfService>();
builder.Services.AddScoped<GoodsIssueService>();
builder.Services.AddScoped<SalesGoodsIssueService>();
builder.Services.AddScoped<SalesOrderWorkflowStatusResolver>();
builder.Services.AddScoped<GoodsIssuePdfService>();
builder.Services.AddScoped<StockMovementService>();
builder.Services.AddScoped<DocumentNumberAllocator>();
builder.Services.AddScoped<SalesInvoiceFromDeliveryChallanService>();
builder.Services.AddScoped<SalesReturnQiBomService>();
builder.Services.AddScoped<SalesReturnQiPostingService>();
builder.Services.AddScoped<SalesReturnCreditMemoPdfService>();
builder.Services.AddScoped<CompanyInfoService>();
builder.Services.AddScoped<EmporiumWalkInCustomerService>();

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Ensure runtime database schema is aligned with EF migrations for the active
// connection string (prevents missing-table errors when environment DB differs).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false);
}

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
