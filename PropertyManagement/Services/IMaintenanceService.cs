using PropertyManagement.Models;

namespace PropertyManagement.Services;

/// <summary>
/// Defines data operations for maintenance work orders.
/// The workflow is: Open → Assigned → InProgress → Completed (or Cancelled at any point).
/// </summary>
public interface IMaintenanceService
{
    /// <summary>
    /// Returns all maintenance requests with Property, Unit, SubmittedBy,
    /// and AssignedTo navigation properties loaded — used by admins and managers.
    /// </summary>
    Task<List<MaintenanceRequest>> GetAllAsync();

    /// <summary>
    /// Returns only the requests assigned to a specific user — used by
    /// MaintenanceStaff who should only see their own work orders.
    /// </summary>
    Task<List<MaintenanceRequest>> GetAssignedToAsync(int userId);

    /// <summary>
    /// Persists a new maintenance request with status set to Open.
    /// </summary>
    Task CreateAsync(MaintenanceRequest request);

    /// <summary>
    /// Assigns an open work order to a staff member, setting status to Assigned
    /// and recording the priority and any notes from the manager.
    /// </summary>
    Task AssignAsync(int requestId, int assignedToUserId, string priority, string notes);

    /// <summary>
    /// Updates the status and completion details (date, cost, materials, notes)
    /// of an existing work order. Used by maintenance staff to progress or complete jobs.
    /// </summary>
    Task UpdateAsync(MaintenanceRequest request);
}
