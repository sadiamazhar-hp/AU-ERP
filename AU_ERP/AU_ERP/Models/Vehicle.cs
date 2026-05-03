using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models;

public static class VehicleCarTypes
{
    public const string Car = "Car";
    public const string MiniTruck = "Mini Truck";
    public const string Truck = "Truck";
    public const string Container = "Container";

    public static readonly string[] All = { Car, MiniTruck, Truck, Container };
}

public class Vehicle
{
    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string NumberPlate { get; set; } = null!;

    [Required]
    [MaxLength(32)]
    public string CarType { get; set; } = VehicleCarTypes.Car;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
