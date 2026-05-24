using System.Security.Claims;
using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>Creates minimal walk-in business partners for users assigned to Emporium plant (Store department).</summary>
public sealed class EmporiumWalkInCustomerService
{
    public const string WalkInSchemaTitle = "WalkIn";

    private readonly AppDbContext _db;

    public EmporiumWalkInCustomerService(AppDbContext db) => _db = db;

    public async Task<int?> GetWalkInSalesSchemaIdAsync(CancellationToken ct = default) =>
        await _db.ConfigurationSchemas.AsNoTracking()
            .Where(s => s.SchemaType == ConfigurationSchemaType.Sales
                        && s.Title.ToLower() == WalkInSchemaTitle.ToLower())
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    /// <summary>Creates a customer BP with WalkIn sales schema. Caller must ensure <see cref="UserPlantResolution.HasEmporiumStorePlant"/>.</summary>
    public async Task<(bool Ok, string? Error, string? BpId, string? DisplayName)> CreateWalkInCustomerAsync(
        ClaimsPrincipal user,
        string? firstName,
        string? lastName,
        string? address,
        string? mobile,
        CancellationToken ct = default)
    {
        if (!UserPlantResolution.HasEmporiumStorePlant(user))
            return (false, "Walk-in customer is only available for users assigned to Emporium plant.", null, null);

        var fn = (firstName ?? "").Trim();
        var ln = (lastName ?? "").Trim();
        var addr = (address ?? "").Trim();
        var mob = (mobile ?? "").Trim();
        if (fn.Length == 0 || ln.Length == 0)
            return (false, "First name and last name are required.", null, null);
        if (addr.Length == 0)
            return (false, "Address is required.", null, null);
        if (mob.Length == 0)
            return (false, "Mobile number is required.", null, null);

        if (fn.Length > 120) fn = fn[..120];
        if (ln.Length > 120) ln = ln[..120];

        var schemaId = await GetWalkInSalesSchemaIdAsync(ct).ConfigureAwait(false);
        if (schemaId is not > 0)
            return (false, "WalkIn sales schema is not configured in the system.", null, null);

        var roleId = await _db.BPRoles.AsNoTracking()
            .Where(r => r.RoleCode == "FLCU01")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (roleId <= 0)
            return (false, "Customer business partner role is not configured.", null, null);

        var customerTypeId = await _db.BPTypeSamples.AsNoTracking()
            .Where(t => (t.IsActive == null || t.IsActive == true) && t.TypeName.ToLower() == "customer")
            .Select(t => t.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        var localGroupId = await _db.BPGroupings.AsNoTracking()
            .Where(g => g.GroupName.ToLower() == "local")
            .Select(g => g.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (customerTypeId <= 0)
            return (false, "Customer BP type is not configured.", null, null);
        if (localGroupId <= 0)
            return (false, "Local BP grouping is not configured.", null, null);

        var bpId = await AllocateNewBpIdAsync(ct).ConfigureAwait(false);
        var fullName = $"{fn} {ln}".Trim();

        var entity = new BusinessPartnerMasterSample
        {
            BPID = bpId,
            BPRoleId = roleId,
            BPTypeId = customerTypeId,
            BPGroupingId = localGroupId,
            FirstName = fn,
            LastName = ln,
            FullName = fullName,
            Street = addr,
            Mobile = mob,
            SalesSchema = schemaId.Value.ToString(),
            DistChannel = "2",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.BusinessPartnerMasterSamples.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return (true, null, bpId, fullName);
    }

    private async Task<string> AllocateNewBpIdAsync(CancellationToken ct)
    {
        var ids = await _db.BusinessPartnerMasterSamples.Select(x => x.BPID).ToListAsync(ct).ConfigureAwait(false);
        var max = 0;
        foreach (var id in ids)
        {
            if (id.Length > 2 && id.StartsWith("BP", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(id.AsSpan(2), out var n))
                max = Math.Max(max, n);
        }
        return $"BP{(max + 1):D6}";
    }
}
