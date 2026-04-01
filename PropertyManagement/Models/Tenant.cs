using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models;

public class Tenant
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(200)]
    public string EmployerName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string EmployerPhone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string EmergencyContactName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string EmergencyContactPhone { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Lease> Leases { get; set; } = new List<Lease>();

    public string FullName => $"{FirstName} {LastName}";
}
