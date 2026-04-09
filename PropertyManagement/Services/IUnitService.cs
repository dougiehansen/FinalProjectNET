using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IUnitService
{
    Task CreateAsync(Unit unit);
    Task UpdateAsync(Unit unit);
}
