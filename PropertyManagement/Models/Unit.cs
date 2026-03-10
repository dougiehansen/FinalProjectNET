using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models;

public class Unit
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    [Required, MaxLength(50)]
    public string UnitNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    public double FloorArea { get; set; }

    public int Bedrooms { get; set; }

    public int Bathrooms { get; set; }

    public decimal MonthlyRent { get; set; }

    [MaxLength(500)]
    public string Amenities { get; set; } = string.Empty;

    public bool IsOccupied { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
}
