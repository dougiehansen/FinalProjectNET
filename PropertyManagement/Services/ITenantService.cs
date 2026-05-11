using PropertyManagement.Models;

namespace PropertyManagement.Services;

public record TenantHealthScore(string Level, string Label, string Reason);

public interface ITenantService
{
    Task<List<Tenant>> GetAllAsync();
    Task<List<Tenant>> GetByPropertyIdsAsync(HashSet<int> propertyIds);
    Task<Dictionary<int, TenantHealthScore>> GetHealthScoresAsync(IEnumerable<int> tenantIds);
    Task CreateAsync(Tenant tenant);
    Task UpdateAsync(Tenant tenant);
    Task DeactivateAsync(int id);
    Task ActivateAsync(int id);
}
