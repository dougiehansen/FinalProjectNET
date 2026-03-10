using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IPropertyService
{
    Task<List<Property>> GetAllAsync();
    Task<Property?> GetByIdAsync(int id);
    Task<Property> CreateAsync(Property property);
    Task<Property> UpdateAsync(Property property);
    Task DeactivateAsync(int id);
}

public class PropertyService : IPropertyService
{
    private readonly ApplicationDbContext _db;

    public PropertyService(ApplicationDbContext db) => _db = db;

    public Task<List<Property>> GetAllAsync() =>
        _db.Properties.Include(p => p.Units).OrderBy(p => p.Name).ToListAsync();

    public Task<Property?> GetByIdAsync(int id) =>
        _db.Properties.Include(p => p.Units).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Property> CreateAsync(Property property)
    {
        property.CreatedAt = DateTime.UtcNow;
        _db.Properties.Add(property);
        await _db.SaveChangesAsync();
        return property;
    }

    public async Task<Property> UpdateAsync(Property property)
    {
        _db.Properties.Update(property);
        await _db.SaveChangesAsync();
        return property;
    }

    public async Task DeactivateAsync(int id)
    {
        var property = await _db.Properties.FindAsync(id);
        if (property != null)
        {
            property.IsActive = false;
            await _db.SaveChangesAsync();
        }
    }
}
