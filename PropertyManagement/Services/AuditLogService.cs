using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _db;

    public AuditLogService(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(string action, string targetName, string targetEmail, string performedBy, string? details = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action      = action,
            TargetName  = targetName,
            TargetEmail = targetEmail,
            PerformedBy = performedBy,
            Details     = details,
            CreatedAt   = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetRecentAsync(int count = 30) =>
        await _db.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync();
}
