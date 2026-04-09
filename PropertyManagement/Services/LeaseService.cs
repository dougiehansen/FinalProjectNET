using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class LeaseService : ILeaseService
{
    private readonly ApplicationDbContext _db;

    public LeaseService(ApplicationDbContext db) => _db = db;

    public async Task<Dictionary<int, Lease>> GetActiveLeasesByUnitAsync() =>
        await _db.Leases
            .Include(l => l.Tenant)
            .Where(l => l.Status == LeaseStatus.Active)
            .ToDictionaryAsync(l => l.UnitId);
}
