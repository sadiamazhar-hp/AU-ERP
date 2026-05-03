using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models.ViewModels;

public sealed class CompanyInfoEditVm
{
    [Required(ErrorMessage = "Company name is required.")]
    [Display(Name = "Company name")]
    [MaxLength(200)]
    public string CompanyName { get; set; } = "";

    [Display(Name = "Address")]
    [MaxLength(500)]
    public string? Address { get; set; }

    [Display(Name = "City")]
    [MaxLength(100)]
    public string? City { get; set; }

    [Required(ErrorMessage = "Country is required.")]
    [Display(Name = "Country")]
    [MaxLength(100)]
    public string Country { get; set; } = "";

    [Display(Name = "Language")]
    [MaxLength(32)]
    public string? Language { get; set; }

    [Display(Name = "Phone number")]
    [RegularExpression(@"^[\d\s\+\-\(\)]*$", ErrorMessage = "Phone number may only contain digits, spaces, +, -, and parentheses.")]
    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Fax")]
    [RegularExpression(@"^[\d\s\+\-\(\)]*$", ErrorMessage = "Fax may only contain digits, spaces, +, -, and parentheses.")]
    [MaxLength(50)]
    public string? Fax { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    [MaxLength(256)]
    public string Email { get; set; } = "";

    [Display(Name = "Website")]
    [MaxLength(500)]
    public string? Website { get; set; }

    [Display(Name = "Tax number (NTN)")]
    [RegularExpression(@"^[\d\w\-]*$", ErrorMessage = "Tax number may only contain letters, digits, and hyphens.")]
    [MaxLength(50)]
    public string? TaxNumberNtn { get; set; }

    [Display(Name = "GST / tax rate (%)")]
    [Range(typeof(decimal), "0", "100", ErrorMessage = "GST / tax rate must be between 0 and 100 (percent).")]
    public decimal? GstOrTaxRate { get; set; }

    [Display(Name = "Bank country")]
    [MaxLength(100)]
    public string? BankCountry { get; set; }

    [Display(Name = "Bank key / code")]
    [MaxLength(40)]
    public string? BankKey { get; set; }

    [Display(Name = "Bank name")]
    [MaxLength(200)]
    public string? BankName { get; set; }

    [Display(Name = "Bank account number")]
    [RegularExpression(@"^[\d\s]*$", ErrorMessage = "Bank account number may only contain digits and spaces.")]
    [MaxLength(34)]
    public string? BankAccountNumber { get; set; }

    [Display(Name = "Account holder name")]
    [MaxLength(200)]
    public string? BankAccountHolderName { get; set; }

    [Display(Name = "Branch")]
    [MaxLength(120)]
    public string? Branch { get; set; }

    [Display(Name = "Bank address")]
    [MaxLength(500)]
    public string? BankAddress { get; set; }

    public bool CanEdit { get; set; }
}
