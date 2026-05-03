using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

/// <summary>Singleton organizational master (logical row Id = 1).</summary>
public class CompanyInfo
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(32)]
    public string? Language { get; set; }

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [MaxLength(50)]
    public string? Fax { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Website { get; set; }

    [MaxLength(50)]
    public string? TaxNumberNtn { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? GstOrTaxRate { get; set; }

    [MaxLength(100)]
    public string? BankCountry { get; set; }

    [MaxLength(40)]
    public string? BankKey { get; set; }

    [MaxLength(200)]
    public string? BankName { get; set; }

    [MaxLength(34)]
    public string? BankAccountNumber { get; set; }

    [MaxLength(200)]
    public string? BankAccountHolderName { get; set; }

    [MaxLength(120)]
    public string? Branch { get; set; }

    [MaxLength(500)]
    public string? BankAddress { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedByUserId { get; set; }

    [ForeignKey(nameof(UpdatedByUserId))]
    public ApplicationUser? UpdatedByUser { get; set; }
}
