using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models;

public class Property
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [MaxLength(20)]
    public string ZipCode { get; set; } = string.Empty;

    [MaxLength(20)]
    public string ContactPhone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ContactEmail { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
