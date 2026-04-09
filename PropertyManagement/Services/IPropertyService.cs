using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IPropertyService
{
    Task<List<Property>> GetAllAsync();
    Task CreateAsync(Property property);
    Task UpdateAsync(Property property);
    Task DeactivateAsync(int id);
    Task ReactivateAsync(int id);
}
