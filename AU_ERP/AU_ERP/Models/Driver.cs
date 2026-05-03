using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models;

public class Driver
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string? FirstName { get; set; }

    [MaxLength(120)]
    public string? LastName { get; set; }

    [MaxLength(32)]
    public string? CNIC { get; set; }

    [MaxLength(64)]
    public string? LicenceNo { get; set; }

    [MaxLength(50)]
    public string? Mobile { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
