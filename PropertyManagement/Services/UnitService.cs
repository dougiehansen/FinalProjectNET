using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class UnitService : IUnitService
{
    private readonly ApplicationDbContext _db;

    public UnitService(ApplicationDbContext db) => _db = db;

    public async Task CreateAsync(Unit unit)
    {
        unit.CreatedAt = DateTime.UtcNow;
        _db.Units.Add(unit);
        await _db.SaveChangesAsync();
    }

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
