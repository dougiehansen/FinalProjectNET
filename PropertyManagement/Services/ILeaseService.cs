using PropertyManagement.Models;

namespace PropertyManagement.Services;

/// <summary>
/// Defines data operations for lease management.
/// Leases represent the legal agreement between a tenant and a unit.
/// </summary>
public interface ILeaseService
{
    /// <summary>
    /// Returns all leases with Tenant, Unit, and Unit.Property navigation
    /// properties loaded — used by the Leases management page.
    /// </summary>
    Task<List<Lease>> GetAllAsync();

    /// <summary>
    /// Returns active leases keyed by UnitId with Tenant loaded.
    /// Used by the Properties page to show the current occupant on each unit card.
    /// </summary>
    Task<Dictionary<int, Lease>> GetActiveLeasesByUnitAsync();

    /// <summary>
    /// Returns true if the given unit already has an Active lease,
    /// optionally ignoring a specific lease (used when editing to exclude itself).
    /// Prevents double-booking a unit.
    /// </summary>
    Task<bool> UnitHasActiveLease(int unitId, int excludeLeaseId = 0);

    /// <summary>
    /// Persists a new lease and marks the unit as occupied when the lease is Active.
    /// </summary>
    Task CreateAsync(Lease lease);

    /// <summary>
    /// Updates an existing lease's editable fields and keeps unit occupancy
    /// in sync — sets IsOccupied true for Active leases, false for all others.
    /// </summary>
    Task UpdateAsync(Lease lease);
}
