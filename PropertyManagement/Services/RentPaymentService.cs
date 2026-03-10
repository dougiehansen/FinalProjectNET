using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IRentPaymentService
{
    Task<List<RentPayment>> GetAllAsync();
    Task<List<RentPayment>> GetByLeaseAsync(int leaseId);
    Task<RentPayment> RecordPaymentAsync(RentPayment payment);
    Task<List<(Tenant Tenant, Lease Lease, decimal Balance)>> GetDelinquentAccountsAsync();
}

public class RentPaymentService : IRentPaymentService
{
    private readonly ApplicationDbContext _db;

    public RentPaymentService(ApplicationDbContext db) => _db = db;

    public Task<List<RentPayment>> GetAllAsync() =>
        _db.RentPayments
           .Include(p => p.Lease).ThenInclude(l => l.Tenant)
           .Include(p => p.Lease).ThenInclude(l => l.Unit).ThenInclude(u => u.Property)
           .OrderByDescending(p => p.PaymentDate)
           .ToListAsync();

    public Task<List<RentPayment>> GetByLeaseAsync(int leaseId) =>
        _db.RentPayments
           .Where(p => p.LeaseId == leaseId)
           .OrderByDescending(p => p.PaymentDate)
           .ToListAsync();

    public async Task<RentPayment> RecordPaymentAsync(RentPayment payment)
    {
        payment.CreatedAt = DateTime.UtcNow;

        // Calculate outstanding balance
        var lease = await _db.Leases
            .Include(l => l.RentPayments)
            .FirstOrDefaultAsync(l => l.Id == payment.LeaseId);

        if (lease != null)
        {
            var monthsElapsed = Math.Max(0, (int)Math.Ceiling((DateTime.Today - lease.StartDate).TotalDays / 30.0));
            var totalDue = lease.MonthlyRent * monthsElapsed;
            var previousPaid = lease.RentPayments.Sum(p => p.Amount);
            var newBalance = Math.Max(0, totalDue - previousPaid - payment.Amount);
            payment.OutstandingBalance = newBalance;
        }

        _db.RentPayments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task<List<(Tenant Tenant, Lease Lease, decimal Balance)>> GetDelinquentAccountsAsync()
    {
        var activeLeases = await _db.Leases
            .Include(l => l.Tenant)
            .Include(l => l.RentPayments)
            .Where(l => l.Status == LeaseStatus.Active)
            .ToListAsync();

        var delinquent = new List<(Tenant, Lease, decimal)>();
        foreach (var lease in activeLeases)
        {
            var monthsElapsed = Math.Max(0, (int)Math.Ceiling((DateTime.Today - lease.StartDate).TotalDays / 30.0));
            var totalDue = lease.MonthlyRent * monthsElapsed;
            var totalPaid = lease.RentPayments.Sum(p => p.Amount);
            var balance = totalDue - totalPaid;
            if (balance > 0)
                delinquent.Add((lease.Tenant, lease, balance));
        }

        return delinquent;
    }
}
