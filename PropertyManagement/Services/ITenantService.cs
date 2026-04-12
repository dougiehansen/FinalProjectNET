using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface ITenantService
{
    Task<List<Tenant>> GetAllAsync();
    Task CreateAsync(Tenant tenant);
    Task UpdateAsync(Tenant tenant);
    Task DeactivateAsync(int id);
}
