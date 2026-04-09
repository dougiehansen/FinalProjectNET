using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface ILeaseService
{
    Task<Dictionary<int, Lease>> GetActiveLeasesByUnitAsync();
}
