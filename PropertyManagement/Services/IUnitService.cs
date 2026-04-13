using PropertyManagement.Models;

namespace PropertyManagement.Services;

/// <summary>
/// Defines data operations for individual rental units.
/// Units are always managed through their parent Property — they are never
/// created or deleted independently via this service.
/// </summary>
public interface IUnitService
{
    /// <summary>
    /// Returns all active units belonging to a given property,
    /// used to populate the unit dropdown when creating or editing a lease.
    /// </summary>
    Task<List<Unit>> GetByPropertyAsync(int propertyId);

    /// <summary>
    /// Persists a new unit linked to an existing property.
    /// </summary>
    Task CreateAsync(Unit unit);

    /// <summary>
    /// Updates the editable fields of an existing unit.
    /// Occupancy status (IsOccupied) is controlled by LeaseService, not here.
    /// </summary>
    Task UpdateAsync(Unit unit);
}
