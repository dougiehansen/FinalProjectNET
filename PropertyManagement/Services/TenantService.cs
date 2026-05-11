using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _db;

    public TenantService(ApplicationDbContext db) => _db = db;

    public async Task<List<Tenant>> GetAllAsync() =>
        await _db.Tenants.OrderBy(t => t.LastName).ThenBy(t => t.FirstName).ToListAsync();

    public async Task<List<Tenant>> GetByPropertyIdsAsync(HashSet<int> propertyIds) =>
        await _db.Tenants
            .Where(t => _db.Leases.Any(l => l.TenantId == t.Id && propertyIds.Contains(l.Unit.PropertyId)))
            .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
            .ToListAsync();

    public async Task<Dictionary<int, TenantHealthScore>> GetHealthScoresAsync(IEnumerable<int> tenantIds)
    {
        var ids   = tenantIds.ToHashSet();
        var today = DateTime.Today;

        var activeLeases = await _db.Leases
            .Include(l => l.RentPayments)
            .Where(l => ids.Contains(l.TenantId) && l.Status == LeaseStatus.Active)
            .ToListAsync();

        var result = new Dictionary<int, TenantHealthScore>();

        foreach (var id in ids)
        {
            if (!activeLeases.Any(l => l.TenantId == id))
                result[id] = new TenantHealthScore("Amber", "No lease", "No active lease found");
        }

        foreach (var lease in activeLeases)
        {
            var monthsActive  = (int)Math.Max(1, Math.Floor((today - lease.StartDate).TotalDays / 30.44));
            var totalExpected = monthsActive * lease.MonthlyRent;
            var totalPaid     = lease.RentPayments.Sum(p => p.Amount);
            var balance       = Math.Max(0, totalExpected - totalPaid);
            var daysLeft      = (lease.EndDate - today).Days;

            TenantHealthScore score;
            if (balance > 0)
                score = new TenantHealthScore("Red",   "Overdue",        $"€{balance:N0} outstanding balance");
            else if (daysLeft <= 60)
                score = new TenantHealthScore("Amber", "Expiring soon",  $"Lease expires in {daysLeft} days");
            else
                score = new TenantHealthScore("Green", "All clear",      $"Paid up · {daysLeft} days remaining");

            result[lease.TenantId] = score;
        }

        return result;
    }

    public async Task CreateAsync(Tenant tenant)
    {
        tenant.CreatedAt = DateTime.UtcNow;
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tenant tenant)
    {
        var existing = await _db.Tenants.FindAsync(tenant.Id);
        if (existing == null) return;

        existing.FirstName             = tenant.FirstName;
        existing.LastName              = tenant.LastName;
        existing.Email                 = tenant.Email;
        existing.Phone                 = tenant.Phone;
        existing.DateOfBirth           = tenant.DateOfBirth;
        existing.EmployerName          = tenant.EmployerName;
        existing.EmployerPhone         = tenant.EmployerPhone;
        existing.EmergencyContactName  = tenant.EmergencyContactName;
        existing.EmergencyContactPhone = tenant.EmergencyContactPhone;
        existing.Notes                 = tenant.Notes;

        await _db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant != null)
        {
            tenant.IsActive = false;
            await _db.SaveChangesAsync();
        }
    }
}
