using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

/// <summary>
/// Implements <see cref="ILeaseService"/> using EF Core and SQLite.
/// All write operations keep the parent Unit's IsOccupied flag in sync
/// so the Properties page reflects occupancy without a separate query.
/// </summary>
public class LeaseService : ILeaseService
{
    private readonly ApplicationDbContext _db;

    public LeaseService(ApplicationDbContext db) => _db = db;

    /// <inheritdoc/>
    public async Task<List<Lease>> GetAllAsync() =>
        await _db.Leases
            .Include(l => l.Tenant)
            .Include(l => l.Unit)
                .ThenInclude(u => u.Property)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<Dictionary<int, Lease>> GetActiveLeasesByUnitAsync() =>
        await _db.Leases
            .Include(l => l.Tenant)
            .Where(l => l.Status == LeaseStatus.Active)
            .ToDictionaryAsync(l => l.UnitId);

    /// <inheritdoc/>
    public async Task<bool> UnitHasActiveLease(int unitId, int excludeLeaseId = 0) =>
        await _db.Leases.AnyAsync(l =>
            l.UnitId == unitId &&
            l.Status == LeaseStatus.Active &&
            l.Id != excludeLeaseId);

    /// <inheritdoc/>
    public async Task CreateAsync(Lease lease)
    {
        _db.Leases.Add(lease);

        // Keep unit occupancy in sync with lease status
        var unit = await _db.Units.FindAsync(lease.UnitId);
        if (unit != null)
            unit.IsOccupied = lease.Status == LeaseStatus.Active;

        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Lease lease)
    {
        var existing = await _db.Leases.FindAsync(lease.Id);
        if (existing == null) return;

        existing.TenantId        = lease.TenantId;
        existing.UnitId          = lease.UnitId;
        existing.StartDate       = lease.StartDate;
        existing.EndDate         = lease.EndDate;
        existing.MonthlyRent     = lease.MonthlyRent;
        existing.SecurityDeposit = lease.SecurityDeposit;
        existing.Status          = lease.Status;
        existing.Notes           = lease.Notes;

        // Keep unit occupancy in sync with the updated status
        var unit = await _db.Units.FindAsync(lease.UnitId);
        if (unit != null)
            unit.IsOccupied = lease.Status == LeaseStatus.Active;

        await _db.SaveChangesAsync();
    }
}
