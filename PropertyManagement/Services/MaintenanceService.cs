using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IMaintenanceService
{
    Task<List<MaintenanceRequest>> GetAllAsync();
    Task<List<MaintenanceRequest>> GetByStatusAsync(MaintenanceStatus status);
    Task<List<MaintenanceRequest>> GetAssignedToAsync(int userId);
    Task<MaintenanceRequest?> GetByIdAsync(int id);
    Task<MaintenanceRequest> CreateAsync(MaintenanceRequest request);
    Task<MaintenanceRequest> UpdateAsync(MaintenanceRequest request);
    Task AssignAsync(int requestId, int staffUserId, string priority, string notes);
}

public class MaintenanceService : IMaintenanceService
{
    private readonly ApplicationDbContext _db;

    public MaintenanceService(ApplicationDbContext db) => _db = db;

    public Task<List<MaintenanceRequest>> GetAllAsync() =>
        _db.MaintenanceRequests
           .Include(m => m.Property)
           .Include(m => m.Unit)
           .Include(m => m.SubmittedBy)
           .Include(m => m.AssignedTo)
           .OrderByDescending(m => m.CreatedAt)
           .ToListAsync();

    public Task<List<MaintenanceRequest>> GetByStatusAsync(MaintenanceStatus status) =>
        _db.MaintenanceRequests
           .Include(m => m.Property)
           .Include(m => m.Unit)
           .Include(m => m.SubmittedBy)
           .Include(m => m.AssignedTo)
           .Where(m => m.Status == status)
           .OrderByDescending(m => m.CreatedAt)
           .ToListAsync();

    public Task<List<MaintenanceRequest>> GetAssignedToAsync(int userId) =>
        _db.MaintenanceRequests
           .Include(m => m.Property)
           .Include(m => m.Unit)
           .Include(m => m.SubmittedBy)
           .Where(m => m.AssignedToUserId == userId)
           .OrderByDescending(m => m.CreatedAt)
           .ToListAsync();

    public Task<MaintenanceRequest?> GetByIdAsync(int id) =>
        _db.MaintenanceRequests
           .Include(m => m.Property)
           .Include(m => m.Unit)
           .Include(m => m.SubmittedBy)
           .Include(m => m.AssignedTo)
           .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<MaintenanceRequest> CreateAsync(MaintenanceRequest request)
    {
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.Status = MaintenanceStatus.Open;
        _db.MaintenanceRequests.Add(request);
        await _db.SaveChangesAsync();
        return request;
    }

    public async Task<MaintenanceRequest> UpdateAsync(MaintenanceRequest request)
    {
        request.UpdatedAt = DateTime.UtcNow;
        _db.MaintenanceRequests.Update(request);
        await _db.SaveChangesAsync();
        return request;
    }

    public async Task AssignAsync(int requestId, int staffUserId, string priority, string notes)
    {
        var request = await _db.MaintenanceRequests.FindAsync(requestId);
        if (request == null) return;

        request.AssignedToUserId = staffUserId;
        request.Priority = priority;
        request.AssignmentNotes = notes;
        request.Status = MaintenanceStatus.Assigned;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
