using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

/// <summary>
/// Implements <see cref="IRentPaymentService"/> using EF Core and SQLite.
/// The outstanding balance stored on each payment reflects the cumulative
/// debt for that lease at the moment the payment was recorded.
/// </summary>
public class RentPaymentService : IRentPaymentService
{
    private readonly ApplicationDbContext _db;

    public RentPaymentService(ApplicationDbContext db) => _db = db;

    /// <inheritdoc/>
    public async Task<List<RentPayment>> GetAllAsync() =>
        await _db.RentPayments
            .Include(p => p.Lease)
                .ThenInclude(l => l.Tenant)
            .Include(p => p.Lease)
                .ThenInclude(l => l.Unit)
                    .ThenInclude(u => u.Property)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<List<RentPayment>> GetRecentByLeaseIdAsync(int leaseId, int count = 3) =>
        await _db.RentPayments
            .Where(p => p.LeaseId == leaseId)
            .OrderByDescending(p => p.PaymentDate)
            .Take(count)
            .ToListAsync();

    /// <inheritdoc/>
    public async Task RecordPaymentAsync(RentPayment payment)
    {
        var lease = await _db.Leases
            .Include(l => l.RentPayments)
            .FirstOrDefaultAsync(l => l.Id == payment.LeaseId);

        if (lease != null)
        {
            var monthsActive = (int)Math.Max(1,
                Math.Floor((DateTime.Today - lease.StartDate).TotalDays / 30.44));
            var totalExpected = monthsActive * lease.MonthlyRent;
            var totalPaid = lease.RentPayments.Sum(p => p.Amount) + payment.Amount;
            payment.OutstandingBalance = Math.Max(0, totalExpected - totalPaid);
        }

        _db.RentPayments.Add(payment);
        await _db.SaveChangesAsync();
    }
}
