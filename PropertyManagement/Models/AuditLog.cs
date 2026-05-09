namespace PropertyManagement.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string TargetEmail { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
