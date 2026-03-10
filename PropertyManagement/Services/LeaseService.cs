using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface ILeaseService
{
    Task<List<Lease>> GetAllAsync();
    Task<Lease?> GetByIdAsync(int id);
    Task<bool> UnitHasActiveLease(int unitId, int? excludeLeaseId = null);
    Task<Lease> CreateAsync(Lease lease);
    Task<Lease> UpdateAsync(Lease lease);
    Task<List<Lease>> GetExpiringSoonAsync(int daysAhead = 60);
    Task<decimal> GetOutstandingBalanceAsync(int leaseId);
}

public class LeaseService : ILeaseService
{
    private readonly ApplicationDbContext _db;

    public LeaseService(ApplicationDbContext db) => _db = db;

    public Task<List<Lease>> GetAllAsync() =>
        _db.Leases
           .Include(l => l.Tenant)
           .Include(l => l.Unit).ThenInclude(u => u.Property)
           .OrderByDescending(l => l.StartDate)
           .ToListAsync();

    public Task<Lease?> GetByIdAsync(int id) =>
        _db.Leases
           .Include(l => l.Tenant)
           .Include(l => l.Unit).ThenInclude(u => u.Property)
           .Include(l => l.RentPayments)
           .FirstOrDefaultAsync(l => l.Id == id);

    public Task<bool> UnitHasActiveLease(int unitId, int? excludeLeaseId = null) =>
        _db.Leases.AnyAsync(l =>
            l.UnitId == unitId &&
            l.Status == LeaseStatus.Active &&
            (excludeLeaseId == null || l.Id != excludeLeaseId));

    public async Task<Lease> CreateAsync(Lease lease)
    {
        lease.CreatedAt = DateTime.UtcNow;
        _db.Leases.Add(lease);

        // Mark unit as occupied
        var unit = await _db.Units.FindAsync(lease.UnitId);
        if (unit != null) unit.IsOccupied = true;

        await _db.SaveChangesAsync();
        return lease;
    }

    public async Task<Lease> UpdateAsync(Lease lease)
    {
        _db.Leases.Update(lease);

        // Update unit occupancy based on lease status
        var unit = await _db.Units.FindAsync(lease.UnitId);
        if (unit != null)
            unit.IsOccupied = lease.Status == LeaseStatus.Active;

        await _db.SaveChangesAsync();
        return lease;
    }

    public Task<List<Lease>> GetExpiringSoonAsync(int daysAhead = 60) =>
        _db.Leases
           .Include(l => l.Tenant)
           .Include(l => l.Unit).ThenInclude(u => u.Property)
           .Where(l => l.Status == LeaseStatus.Active && l.EndDate <= DateTime.Today.AddDays(daysAhead))
           .OrderBy(l => l.EndDate)
           .ToListAsync();

    public async Task<decimal> GetOutstandingBalanceAsync(int leaseId)
    {
        var lease = await _db.Leases.Include(l => l.RentPayments).FirstOrDefaultAsync(l => l.Id == leaseId);
        if (lease == null) return 0;

        var monthsElapsed = Math.Max(0, (int)Math.Ceiling((DateTime.Today - lease.StartDate).TotalDays / 30.0));
        var totalDue = lease.MonthlyRent * monthsElapsed;
        var totalPaid = lease.RentPayments.Sum(p => p.Amount);
        return Math.Max(0, totalDue - totalPaid);
    }
}
