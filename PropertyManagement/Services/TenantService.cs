using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _db;

    public TenantService(ApplicationDbContext db) => _db = db;

    public async Task<List<Tenant>> GetAllAsync() =>
        await _db.Tenants.OrderBy(t => t.LastName).ThenBy(t => t.FirstName).ToListAsync();

    public async Task<List<Tenant>> GetByPropertyIdsAsync(HashSet<int> propertyIds) =>
        await _db.Tenants
            .Where(t => _db.Leases.Any(l => l.TenantId == t.Id && propertyIds.Contains(l.Unit.PropertyId)))
            .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
            .ToListAsync();

    public async Task CreateAsync(Tenant tenant)
    {
        tenant.CreatedAt = DateTime.UtcNow;
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tenant tenant)
    {
        var existing = await _db.Tenants.FindAsync(tenant.Id);
        if (existing == null) return;

        existing.FirstName             = tenant.FirstName;
        existing.LastName              = tenant.LastName;
        existing.Email                 = tenant.Email;
        existing.Phone                 = tenant.Phone;
        existing.DateOfBirth           = tenant.DateOfBirth;
        existing.EmployerName          = tenant.EmployerName;
        existing.EmployerPhone         = tenant.EmployerPhone;
        existing.EmergencyContactName  = tenant.EmergencyContactName;
        existing.EmergencyContactPhone = tenant.EmergencyContactPhone;
        existing.Notes                 = tenant.Notes;

        await _db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant != null)
        {
            tenant.IsActive = false;
            await _db.SaveChangesAsync();
        }
    }
}
