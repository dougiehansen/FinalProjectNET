using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;

namespace PropertyManagement.Services;

public class PropertyAccessService : IPropertyAccessService
{
    private readonly ApplicationDbContext _db;

    public PropertyAccessService(ApplicationDbContext db) => _db = db;

    public async Task<HashSet<int>?> GetAllowedIdsAsync(ClaimsPrincipal user)
    {
        if (user.IsInRole("Administrator")) return null;

        if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return null;

        var ids = await _db.UserPropertyAssignments
            .Where(a => a.UserId == userId)
            .Select(a => a.PropertyId)
            .ToListAsync();

        return ids.Count == 0 ? null : ids.ToHashSet();
    }

    public bool CanAccess(HashSet<int>? allowed, int propertyId) =>
        allowed == null || allowed.Contains(propertyId);
}
