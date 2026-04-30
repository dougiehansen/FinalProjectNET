using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db) => _db = db;

    public async Task<List<OccupancyRow>> GetOccupancySummaryAsync(int propertyId)
    {
        var query = _db.Units
            .Include(u => u.Property)
            .Where(u => u.IsActive && u.Property.IsActive);

        if (propertyId > 0)
            query = query.Where(u => u.PropertyId == propertyId);

        var units = await query.ToListAsync();

        return units
            .GroupBy(u => u.Property)
            .Select(g => new OccupancyRow
            {
                PropertyName     = g.Key.Name,
                PropertyCity     = string.IsNullOrEmpty(g.Key.City) ? g.Key.State : g.Key.City,
                TotalUnits       = g.Count(),
                OccupiedUnits    = g.Count(u => u.IsOccupied),
                PotentialRevenue = g.Sum(u => u.MonthlyRent),
                ActualRevenue    = g.Where(u => u.IsOccupied).Sum(u => u.MonthlyRent)
            })
            .OrderBy(r => r.PropertyName)
            .ToList();
    }

    public async Task<List<RentRollRow>> GetRentRollAsync(int propertyId)
    {
        var query = _db.Leases
            .Include(l => l.Tenant)
            .Include(l => l.Unit).ThenInclude(u => u.Property)
            .Where(l => l.Status == LeaseStatus.Active);

        if (propertyId > 0)
            query = query.Where(l => l.Unit.PropertyId == propertyId);

        var leases = await query.OrderBy(l => l.Unit.Property.Name).ThenBy(l => l.Unit.UnitNumber).ToListAsync();

        return leases.Select(l => new RentRollRow
        {
            PropertyName = l.Unit.Property.Name,
            UnitNumber   = l.Unit.UnitNumber,
            TenantName   = $"{l.Tenant.FirstName} {l.Tenant.LastName}",
            MonthlyRent  = l.MonthlyRent,
            LeaseStart   = l.StartDate,
            LeaseEnd     = l.EndDate,
            Status       = l.Status
        }).ToList();
    }

    public async Task<List<OutstandingPaymentRow>> GetOutstandingPaymentsAsync(int propertyId)
    {
        var query = _db.Leases
            .Include(l => l.Tenant)
            .Include(l => l.Unit).ThenInclude(u => u.Property)
            .Include(l => l.RentPayments)
            .Where(l => l.Status == LeaseStatus.Active);

        if (propertyId > 0)
            query = query.Where(l => l.Unit.PropertyId == propertyId);

        var leases = await query.ToListAsync();

        return leases
            .Select(l => new
            {
                Lease   = l,
                Balance = l.RentPayments.Any()
                    ? l.RentPayments.OrderByDescending(p => p.PaymentDate).First().OutstandingBalance
                    : l.MonthlyRent
            })
            .Where(x => x.Balance > 0)
            .Select(x => new OutstandingPaymentRow
            {
                TenantName         = $"{x.Lease.Tenant.FirstName} {x.Lease.Tenant.LastName}",
                PropertyName       = x.Lease.Unit.Property.Name,
                UnitNumber         = x.Lease.Unit.UnitNumber,
                MonthlyRent        = x.Lease.MonthlyRent,
                OutstandingBalance = x.Balance,
                LeaseEnd           = x.Lease.EndDate
            })
            .OrderByDescending(r => r.OutstandingBalance)
            .ToList();
    }

    public async Task<List<MaintenanceLogRow>> GetMaintenanceLogAsync(int propertyId, DateTime? from, DateTime? to)
    {
        var query = _db.MaintenanceRequests
            .Include(m => m.Property)
            .Include(m => m.Unit)
            .AsQueryable();

        if (propertyId > 0)
            query = query.Where(m => m.PropertyId == propertyId);

        if (from.HasValue)
            query = query.Where(m => m.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(m => m.CreatedAt <= to.Value.AddDays(1));

        var requests = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

        return requests.Select(m => new MaintenanceLogRow
        {
            PropertyName   = m.Property.Name,
            UnitNumber     = m.Unit.UnitNumber,
            Title          = m.Title,
            UrgencyLevel   = m.UrgencyLevel,
            Status         = m.Status,
            CreatedAt      = m.CreatedAt,
            CompletionDate = m.CompletionDate,
            EstimatedCost  = m.EstimatedCost
        }).ToList();
    }
}
