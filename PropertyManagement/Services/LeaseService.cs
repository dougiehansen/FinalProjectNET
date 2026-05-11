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
            .AsNoTracking()
            .Include(l => l.Tenant)
            .Include(l => l.Unit)
                .ThenInclude(u => u.Property)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<List<Lease>> GetByTenantIdAsync(int tenantId) =>
        await _db.Leases
            .AsNoTracking()
            .Include(l => l.Unit).ThenInclude(u => u.Property)
            .Include(l => l.RentPayments)
            .Where(l => l.TenantId == tenantId)
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
    public async Task<decimal> GetOutstandingBalanceAsync(int leaseId)
    {
        var lease = await _db.Leases
            .Include(l => l.RentPayments)
            .FirstOrDefaultAsync(l => l.Id == leaseId);

        if (lease == null) return 0;

        var monthsActive = (int)Math.Max(1,
            Math.Floor((DateTime.Today - lease.StartDate).TotalDays / 30.44));
        var totalExpected = monthsActive * lease.MonthlyRent;
        var totalPaid = lease.RentPayments.Sum(p => p.Amount);
        return Math.Max(0, totalExpected - totalPaid);
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

    /// <inheritdoc/>
    public async Task<int> AutoExpireAsync()
    {
        var expired = await _db.Leases
            .Where(l => l.Status == LeaseStatus.Active && l.EndDate < DateTime.Today)
            .Include(l => l.Unit)
            .ToListAsync();

        foreach (var lease in expired)
        {
            lease.Status = LeaseStatus.Expired;
            if (lease.Unit != null)
                lease.Unit.IsOccupied = false;
        }

        if (expired.Count > 0)
            await _db.SaveChangesAsync();

        return expired.Count;
    }

    /// <inheritdoc/>
    public async Task<Lease?> GetByTokenAsync(string token) =>
        await _db.Leases
            .Include(l => l.Tenant)
            .Include(l => l.Unit)
                .ThenInclude(u => u.Property)
            .FirstOrDefaultAsync(l => l.SigningToken == token);

    /// <inheritdoc/>
    public async Task SignAsync(string token, string signedByName)
    {
        var lease = await _db.Leases.FirstOrDefaultAsync(l => l.SigningToken == token);
        if (lease == null) return;

        lease.SignatureStatus = SignatureStatus.Signed;
        lease.SignedByName    = signedByName;
        lease.SignedAt        = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task DeclineAsync(string token)
    {
        var lease = await _db.Leases.FirstOrDefaultAsync(l => l.SigningToken == token);
        if (lease == null) return;

        lease.SignatureStatus = SignatureStatus.Declined;
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task RecordSigningOpenedAsync(string token, string? ipAddress, string? userAgent)
    {
        var lease = await _db.Leases.FirstOrDefaultAsync(l => l.SigningToken == token);
        if (lease is null || lease.SignatureStatus != SignatureStatus.Pending) return;
        lease.SigningPageOpenedAt = DateTime.UtcNow;
        lease.TenantIpAddress    = ipAddress;
        lease.TenantUserAgent    = userAgent;
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task ConfirmReviewAsync(int leaseId)
    {
        var lease = await _db.Leases.FindAsync(leaseId);
        if (lease == null) return;

        lease.ManagerConfirmed = true;
        await _db.SaveChangesAsync();
    }
}
