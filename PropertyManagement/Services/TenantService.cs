using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface ITenantService
{
    Task<List<Tenant>> GetAllAsync();
    Task<Tenant?> GetByIdAsync(int id);
    Task<Tenant> CreateAsync(Tenant tenant);
    Task<Tenant> UpdateAsync(Tenant tenant);
    Task DeactivateAsync(int id);
}

public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _db;

    public TenantService(ApplicationDbContext db) => _db = db;

    public Task<List<Tenant>> GetAllAsync() =>
        _db.Tenants.Where(t => t.IsActive).OrderBy(t => t.LastName).ThenBy(t => t.FirstName).ToListAsync();

    public Task<Tenant?> GetByIdAsync(int id) =>
        _db.Tenants.Include(t => t.Leases).ThenInclude(l => l.Unit).ThenInclude(u => u.Property)
                   .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Tenant> CreateAsync(Tenant tenant)
    {
        tenant.CreatedAt = DateTime.UtcNow;
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();
        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant)
    {
        _db.Tenants.Update(tenant);
        await _db.SaveChangesAsync();
        return tenant;
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
