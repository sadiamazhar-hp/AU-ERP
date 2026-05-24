using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AU_ERP.Models;
using AU_ERP.Models.Mobile;
using AU_ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AU_ERP.Controllers.Mobile;

[ApiController]
[Route("api/mobile/auth")]
[AllowAnonymous]
public class MobileAuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public MobileAuthController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        AppDbContext db,
        IConfiguration config)
    {
        _signIn = signIn;
        _users = users;
        _db = db;
        _config = config;
    }

    /// <summary>Login with email and password. Returns a JWT for mobile API access.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<MobileApiResponse<MobileLoginResponse>>> Login(
        [FromBody] MobileLoginRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(MobileApiResponse<MobileLoginResponse>.Fail("Email and password are required."));

        var user = await _users.FindByEmailAsync(request.Email.Trim());
        if (user == null)
            return Unauthorized(MobileApiResponse<MobileLoginResponse>.Fail("Invalid email or password."));

        if (!user.PasswordSetupCompleted || string.IsNullOrEmpty(user.PasswordHash))
            return Unauthorized(MobileApiResponse<MobileLoginResponse>.Fail(
                "You have not set your password yet. Use the link in your invitation email, or use Forgot password on the web app."));

        // Match web AccountController: sign-in uses UserName, not email string.
        var result = await _signIn.PasswordSignInAsync(
            user.UserName!, request.Password, isPersistent: false, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized(MobileApiResponse<MobileLoginResponse>.Fail("Invalid email or password."));

        var departments = await _db.ApplicationUserDepartments
            .AsNoTracking()
            .Where(ud => ud.UserId == user.Id)
            .Select(ud => ud.Department!.Code)
            .ToListAsync(ct);

        var token = BuildJwt(user, departments);
        var expiryDays = _config.GetValue<int>("Jwt:ExpirationDays", 30);
        var expiresAt = DateTime.UtcNow.AddDays(expiryDays);

        var displayName = string.Join(" ", new[] { user.FirstName, user.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(displayName)) displayName = user.Email ?? user.UserName ?? "User";

        var response = new MobileLoginResponse(
            Token: token,
            ExpiresAt: expiresAt,
            UserId: user.Id,
            Email: user.Email ?? "",
            DisplayName: displayName,
            Departments: departments
        );

        return Ok(MobileApiResponse<MobileLoginResponse>.Ok(response));
    }

    /// <summary>Returns profile of the currently authenticated user.</summary>
    [HttpGet("me")]
    [Authorize(Policy = "MobileApi")]
    public async Task<ActionResult<MobileApiResponse<MobileUserProfile>>> Me(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _users.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var departments = await _db.ApplicationUserDepartments
            .AsNoTracking()
            .Where(ud => ud.UserId == user.Id)
            .Select(ud => ud.Department!.Code)
            .ToListAsync(ct);

        var displayName = string.Join(" ", new[] { user.FirstName, user.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(displayName)) displayName = user.Email ?? user.UserName ?? "User";

        return Ok(MobileApiResponse<MobileUserProfile>.Ok(new MobileUserProfile(
            UserId: user.Id,
            Email: user.Email ?? "",
            DisplayName: displayName,
            Departments: departments
        )));
    }

    // ─────────────────────────────────────────────────────────────────────────

    private string BuildJwt(ApplicationUser user, List<string> departments)
    {
        var secret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var expiryDays = _config.GetValue<int>("Jwt:ExpirationDays", 30);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var dept in departments)
            claims.Add(new Claim(AuClaimTypes.Department, dept));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
