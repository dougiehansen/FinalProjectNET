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
        var duplicate = await _db.Properties.AnyAsync(p => p.Name == property.Name && p.Address == property.Address);
        if (duplicate) throw new InvalidOperationException($"A property named '{property.Name}' at that address already exists.");

        property.CreatedAt = DateTime.UtcNow;
        _db.Properties.Add(property);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Property property)
    {
        var existing = await _db.Properties.FindAsync(property.Id);
        if (existing == null) return;

        var duplicate = await _db.Properties.AnyAsync(p => p.Name == property.Name && p.Address == property.Address && p.Id != property.Id);
        if (duplicate) throw new InvalidOperationException($"A property named '{property.Name}' at that address already exists.");

        existing.Name         = property.Name;
        existing.Address      = property.Address;
        existing.City         = property.City;
        existing.State        = property.State;
        existing.ZipCode      = property.ZipCode;
        existing.ContactPhone = property.ContactPhone;
        existing.ContactEmail = property.ContactEmail;
        existing.ImagePath    = property.ImagePath;
        existing.Latitude     = property.Latitude;
        existing.Longitude    = property.Longitude;

        await _db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int id)
    {
        var hasActiveLeases = await _db.Leases.AnyAsync(l => l.Unit.PropertyId == id && l.Status == LeaseStatus.Active);
        if (hasActiveLeases) throw new InvalidOperationException("Cannot deactivate a property that has active leases. End all leases first.");

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
