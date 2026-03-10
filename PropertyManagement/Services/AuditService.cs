using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IAuditService
{
    Task LogAsync(int userId, string userEmail, string action, string entityType, int? entityId = null, string details = "");
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(int userId, string userEmail, string action, string entityType, int? entityId = null, string details = "")
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            UserEmail = userEmail,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
