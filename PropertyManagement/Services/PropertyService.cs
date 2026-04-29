using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class PropertyService : IPropertyService
{
    private readonly ApplicationDbContext _db;

    public PropertyService(ApplicationDbContext db) => _db = db;

    public async Task<List<Property>> GetAllAsync() =>
        await _db.Properties
            .Include(p => p.Units)
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task CreateAsync(Property property)
    {
        property.CreatedAt = DateTime.UtcNow;
        _db.Properties.Add(property);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Property property)
    {
        var existing = await _db.Properties.FindAsync(property.Id);
        if (existing == null) return;

        existing.Name         = property.Name;
        existing.Address      = property.Address;
        existing.City         = property.City;
        existing.State        = property.State;
        existing.ZipCode      = property.ZipCode;
        existing.ContactPhone = property.ContactPhone;
        existing.ContactEmail = property.ContactEmail;
        existing.ImagePath    = property.ImagePath;

        await _db.SaveChangesAsync();
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

    public async Task ReactivateAsync(int id)
    {
        var property = await _db.Properties.FindAsync(id);
        if (property != null)
        {
            property.IsActive = true;
            await _db.SaveChangesAsync();
        }
    }
}
