using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IAuditLogService
{
    Task LogAsync(string action, string targetName, string targetEmail, string performedBy, string? details = null);
    Task<List<AuditLog>> GetRecentAsync(int count = 200);
    Task<int> GetTodayCountAsync();
}
