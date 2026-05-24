using System.Text;
using AU_ERP.Models;
using AU_ERP.Services;
using AU_ERP.Services.Mobile;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity ──────────────────────────────────────────────────────────────────
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

// ── Cookie auth (web app) ─────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(10);

    // Return 401/403 instead of redirecting for /api/* requests.
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// ── JWT auth (mobile API) ─────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

// ── Authorization policies ────────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminDepartment", policy =>
        policy.RequireClaim(AuClaimTypes.Department, "Admin"));
    options.AddPolicy("ProductionDepartment", policy =>
        policy.RequireClaim(AuClaimTypes.Department, "Production"));
    options.AddPolicy("SalesDepartment", policy =>
        policy.RequireClaim(AuClaimTypes.Department, "Sales"));
    options.AddPolicy("FinanceDepartment", policy =>
        policy.RequireClaim(AuClaimTypes.Department, "Finance"));
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

    // Mobile API: any authenticated user (JWT) may access reporting.
    options.AddPolicy("MobileApi", policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
              .RequireAuthenticatedUser());
});

// ── Application services ──────────────────────────────────────────────────────
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
builder.Services.AddScoped<MobileReportingService>();

// ── CORS (React Native dev / production) ──────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileCors", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ── MVC (web app, global auth filter) ────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// ── Swagger (mobile API only) ─────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("mobile", new OpenApiInfo
    {
        Title = "AU ERP Mobile API",
        Version = "v1",
        Description = "Reporting API for the AU ERP mobile app"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without the 'Bearer ' prefix)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    // Only include /api/mobile controllers in Swagger
    c.DocInclusionPredicate((_, api) =>
        api.RelativePath?.StartsWith("api/mobile", StringComparison.OrdinalIgnoreCase) == true);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Ensure runtime database schema is aligned with EF migrations for the active
// connection string (prevents missing-table errors when environment DB differs).
// ── DB migration ──────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false);
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Mobile dev uses http://10.0.2.2:5242; HTTPS redirect breaks emulator API calls.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("MobileCors");
app.UseAuthentication();
app.UseAuthorization();

// Swagger UI at /swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/mobile/swagger.json", "AU ERP Mobile API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Splash}/{id?}");

// Map API controllers (mobile endpoints)
app.MapControllers();

// ── Seed data ─────────────────────────────────────────────────────────────────
await IdentityDataSeeder.SeedAsync(app.Services, app.Configuration).ConfigureAwait(false);
await ConfigurationLookupSeeder.SeedAsync(app.Services).ConfigureAwait(false);

await app.RunAsync().ConfigureAwait(false);
