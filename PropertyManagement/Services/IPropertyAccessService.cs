using System.Security.Claims;

namespace PropertyManagement.Services;

public interface IPropertyAccessService
{
    /// Returns null = unrestricted (all properties). Returns a set = restricted to those IDs.
    /// No assignments on a non-admin user also returns null (all-access by default).
    Task<HashSet<int>?> GetAllowedIdsAsync(ClaimsPrincipal user);

    bool CanAccess(HashSet<int>? allowed, int propertyId);
}
