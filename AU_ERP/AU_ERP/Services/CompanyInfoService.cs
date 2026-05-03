using AU_ERP.Models;
using AU_ERP.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

public sealed class CompanyInfoService
{
    private readonly AppDbContext _db;

    public CompanyInfoService(AppDbContext db) => _db = db;

    public async Task<CompanyInfoEditVm> GetEditVmAsync(bool canEdit, CancellationToken ct = default)
    {
        var row = await _db.CompanyInfos.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == CompanyInfo.SingletonId, ct)
            .ConfigureAwait(false);
        return MapToVm(row, canEdit);
    }

    public static CompanyInfoEditVm MapToVm(CompanyInfo? row, bool canEdit)
    {
        if (row == null)
        {
            return new CompanyInfoEditVm { CanEdit = canEdit };
        }

        return new CompanyInfoEditVm
        {
            CanEdit = canEdit,
            CompanyName = row.CompanyName ?? "",
            Address = row.Address,
            City = row.City,
            Country = row.Country ?? "",
            Language = row.Language,
            PhoneNumber = row.PhoneNumber,
            Fax = row.Fax,
            Email = row.Email ?? "",
            Website = row.Website,
            TaxNumberNtn = row.TaxNumberNtn,
            GstOrTaxRate = row.GstOrTaxRate,
            BankCountry = row.BankCountry,
            BankKey = row.BankKey,
            BankName = row.BankName,
            BankAccountNumber = row.BankAccountNumber,
            BankAccountHolderName = row.BankAccountHolderName,
            Branch = row.Branch,
            BankAddress = row.BankAddress
        };
    }

    /// <summary>Returns null on success, or an error message.</summary>
    public async Task<string?> SaveAsync(CompanyInfoEditVm vm, string? userId, CancellationToken ct = default)
    {
        var web = (vm.Website ?? "").Trim();
        if (web.Length > 0)
        {
            var webForUri = (web.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                             || web.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                ? web
                : "https://" + web;
            if (!Uri.TryCreate(webForUri, UriKind.Absolute, out _))
                return "Website must be a valid URL.";
        }

        var row = await _db.CompanyInfos.FirstOrDefaultAsync(c => c.Id == CompanyInfo.SingletonId, ct)
                  .ConfigureAwait(false);
        if (row == null)
        {
            row = new CompanyInfo { Id = CompanyInfo.SingletonId };
            await _db.CompanyInfos.AddAsync(row, ct).ConfigureAwait(false);
        }

        row.CompanyName = vm.CompanyName.Trim();
        row.Address = NullIfWhiteSpace(vm.Address);
        row.City = NullIfWhiteSpace(vm.City);
        row.Country = vm.Country.Trim();
        row.Language = NullIfWhiteSpace(vm.Language);
        row.PhoneNumber = NullIfWhiteSpace(vm.PhoneNumber);
        row.Fax = NullIfWhiteSpace(vm.Fax);
        row.Email = vm.Email.Trim();
        row.Website = NullIfWhiteSpace(vm.Website);
        row.TaxNumberNtn = NullIfWhiteSpace(vm.TaxNumberNtn);
        row.GstOrTaxRate = vm.GstOrTaxRate;
        row.BankCountry = NullIfWhiteSpace(vm.BankCountry);
        row.BankKey = NullIfWhiteSpace(vm.BankKey);
        row.BankName = NullIfWhiteSpace(vm.BankName);
        row.BankAccountNumber = NormalizeBankAccount(vm.BankAccountNumber);
        row.BankAccountHolderName = NullIfWhiteSpace(vm.BankAccountHolderName);
        row.Branch = NullIfWhiteSpace(vm.Branch);
        row.BankAddress = NullIfWhiteSpace(vm.BankAddress);
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedByUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return null;
    }

    private static string? NullIfWhiteSpace(string? s)
    {
        var t = (s ?? "").Trim();
        return t.Length == 0 ? null : t;
    }

    private static string? NormalizeBankAccount(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        var digits = string.Concat(s.Where(char.IsDigit));
        return digits.Length == 0 ? null : digits;
    }
}
