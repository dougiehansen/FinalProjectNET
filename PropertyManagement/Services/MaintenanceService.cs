using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

/// <summary>
/// Implements <see cref="IMaintenanceService"/> using EF Core and SQLite.
/// All queries eager-load the four navigation properties (Property, Unit,
/// SubmittedBy, AssignedTo) so the page never triggers lazy-load N+1 queries.
/// </summary>
public class MaintenanceService : IMaintenanceService
{
    private readonly ApplicationDbContext _db;

    public MaintenanceService(ApplicationDbContext db) => _db = db;

    /// <inheritdoc/>
    public async Task<List<MaintenanceRequest>> GetAllAsync() =>
        await _db.MaintenanceRequests
            .Include(m => m.Property)
            .Include(m => m.Unit)
            .Include(m => m.SubmittedBy)
            .Include(m => m.AssignedTo)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<List<MaintenanceRequest>> GetAssignedToAsync(int userId) =>
        await _db.MaintenanceRequests
            .Include(m => m.Property)
            .Include(m => m.Unit)
            .Include(m => m.SubmittedBy)
            .Include(m => m.AssignedTo)
            .Where(m => m.AssignedToUserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

    /// <inheritdoc/>
    public async Task CreateAsync(MaintenanceRequest request)
    {
        request.Status    = MaintenanceStatus.Open;
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        _db.MaintenanceRequests.Add(request);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task AssignAsync(int requestId, int assignedToUserId, string priority, string notes)
    {
        var request = await _db.MaintenanceRequests.FindAsync(requestId);
        if (request == null) return;

        request.AssignedToUserId = assignedToUserId;
        request.Priority         = priority;
        request.AssignmentNotes  = notes;
        request.Status           = MaintenanceStatus.Assigned;
        request.UpdatedAt        = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(MaintenanceRequest request)
    {
        var existing = await _db.MaintenanceRequests.FindAsync(request.Id);
        if (existing == null) return;

        existing.Status          = request.Status;
        existing.CompletionDate  = request.CompletionDate;
        existing.EstimatedCost   = request.EstimatedCost;
        existing.MaterialsUsed   = request.MaterialsUsed;
        existing.CompletionNotes = request.CompletionNotes;
        existing.UpdatedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}
