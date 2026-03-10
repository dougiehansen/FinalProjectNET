using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IUnitService
{
    Task<List<Unit>> GetByPropertyAsync(int propertyId);
    Task<Unit?> GetByIdAsync(int id);
    Task<Unit> CreateAsync(Unit unit);
    Task<Unit> UpdateAsync(Unit unit);
}

public class UnitService : IUnitService
{
    private readonly ApplicationDbContext _db;

    public UnitService(ApplicationDbContext db) => _db = db;

    public Task<List<Unit>> GetByPropertyAsync(int propertyId) =>
        _db.Units.Where(u => u.PropertyId == propertyId && u.IsActive)
                 .OrderBy(u => u.UnitNumber)
                 .ToListAsync();

    public Task<Unit?> GetByIdAsync(int id) =>
        _db.Units.Include(u => u.Property).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<Unit> CreateAsync(Unit unit)
    {
        unit.CreatedAt = DateTime.UtcNow;
        _db.Units.Add(unit);
        await _db.SaveChangesAsync();
        return unit;
    }

    public async Task<Unit> UpdateAsync(Unit unit)
    {
        _db.Units.Update(unit);
        await _db.SaveChangesAsync();
        return unit;
    }
}
