using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models;

public class AuditLog
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [MaxLength(200)]
    public string UserEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    [MaxLength(2000)]
    public string Details { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
