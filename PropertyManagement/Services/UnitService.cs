using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

/// <summary>
/// Implements <see cref="IUnitService"/> using EF Core and SQLite.
/// Note: IsOccupied is intentionally excluded from UpdateAsync — occupancy
/// is managed exclusively by LeaseService to keep the two in sync.
/// </summary>
public class UnitService : IUnitService
{
    private readonly ApplicationDbContext _db;

    public UnitService(ApplicationDbContext db) => _db = db;

    /// <inheritdoc/>
    public async Task<List<Unit>> GetByPropertyAsync(int propertyId) =>
        await _db.Units
            .Where(u => u.PropertyId == propertyId && u.IsActive)
            .OrderBy(u => u.UnitNumber)
            .ToListAsync();

    /// <inheritdoc/>
    public async Task CreateAsync(Unit unit)
    {
        unit.CreatedAt = DateTime.UtcNow;
        _db.Units.Add(unit);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Unit unit)
    {
        var existing = await _db.Units.FindAsync(unit.Id);
        if (existing == null) return;

        existing.UnitNumber  = unit.UnitNumber;
        existing.Type        = unit.Type;
        existing.FloorArea   = unit.FloorArea;
        existing.Bedrooms    = unit.Bedrooms;
        existing.Bathrooms   = unit.Bathrooms;
        existing.MonthlyRent = unit.MonthlyRent;
        existing.Amenities   = unit.Amenities;

        await _db.SaveChangesAsync();
    }
}
