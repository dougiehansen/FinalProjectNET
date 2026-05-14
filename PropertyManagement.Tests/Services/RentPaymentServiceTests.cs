using PropertyManagement.Models;
using PropertyManagement.Services;
using PropertyManagement.Tests.Helpers;

namespace PropertyManagement.Tests.Services;

public class RentPaymentServiceTests
{
    static void SeedLease(PropertyManagement.Data.ApplicationDbContext db, DateTime start)
    {
        db.Properties.Add(new Property { Id = 1, Name = "P", Address = "1 St", City = "C", IsActive = true });
        db.Units.Add(new Unit    { Id = 1, PropertyId = 1, UnitNumber = "A", MonthlyRent = 1000, IsActive = true });
        db.Tenants.Add(new Tenant { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com" });
        db.Leases.Add(new Lease  { Id = 1, TenantId = 1, UnitId = 1, StartDate = start, EndDate = start.AddYears(1), MonthlyRent = 1000, Status = LeaseStatus.Active });
        db.SaveChanges();
    }

    // ── RecordPaymentAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RecordPayment_PersistsPaymentAndSetsBalance()
    {
        using var db = DbHelper.CreateDb(nameof(RecordPayment_PersistsPaymentAndSetsBalance));
        SeedLease(db, DateTime.Today.AddMonths(-2));

        var payment = new RentPayment { LeaseId = 1, Amount = 500, PaymentDate = DateTime.Today };
        await new RentPaymentService(db).RecordPaymentAsync(payment);

        var saved = db.RentPayments.Single();
        Assert.Equal(500, saved.Amount);
        Assert.True(saved.OutstandingBalance >= 0);
    }

    [Fact]
    public async Task RecordPayment_OutstandingBalance_IsZero_WhenOverpaid()
    {
        using var db = DbHelper.CreateDb(nameof(RecordPayment_OutstandingBalance_IsZero_WhenOverpaid));
        var start    = DateTime.Today.AddMonths(-1);
        SeedLease(db, start);

        var payment = new RentPayment { LeaseId = 1, Amount = 99999, PaymentDate = DateTime.Today };
        await new RentPaymentService(db).RecordPaymentAsync(payment);

        Assert.Equal(0, db.RentPayments.Single().OutstandingBalance);
    }

    [Fact]
    public async Task RecordPayment_ReducesOutstandingBalance_WithEachPayment()
    {
        using var db = DbHelper.CreateDb(nameof(RecordPayment_ReducesOutstandingBalance_WithEachPayment));
        SeedLease(db, DateTime.Today.AddMonths(-3));
        var svc = new RentPaymentService(db);

        await svc.RecordPaymentAsync(new RentPayment { LeaseId = 1, Amount = 500,  PaymentDate = DateTime.Today.AddDays(-10) });
        await svc.RecordPaymentAsync(new RentPayment { LeaseId = 1, Amount = 1000, PaymentDate = DateTime.Today });

        var payments  = db.RentPayments.OrderBy(p => p.PaymentDate).ToList();
        Assert.True(payments[1].OutstandingBalance < payments[0].OutstandingBalance);
    }

    // ── GetRecentByLeaseIdAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetRecentByLeaseId_ReturnsRequestedCount()
    {
        using var db = DbHelper.CreateDb(nameof(GetRecentByLeaseId_ReturnsRequestedCount));
        SeedLease(db, DateTime.Today.AddMonths(-6));

        for (int i = 1; i <= 5; i++)
            db.RentPayments.Add(new RentPayment { LeaseId = 1, Amount = 1000, PaymentDate = DateTime.Today.AddMonths(-i) });
        db.SaveChanges();

        var result = await new RentPaymentService(db).GetRecentByLeaseIdAsync(1, 3);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetRecentByLeaseId_ReturnsEmpty_WhenNoPayments()
    {
        using var db = DbHelper.CreateDb(nameof(GetRecentByLeaseId_ReturnsEmpty_WhenNoPayments));
        SeedLease(db, DateTime.Today.AddMonths(-2));

        var result = await new RentPaymentService(db).GetRecentByLeaseIdAsync(1, 5);

        Assert.Empty(result);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllPayments_OrderedByDateDesc()
    {
        using var db = DbHelper.CreateDb(nameof(GetAllAsync_ReturnsAllPayments_OrderedByDateDesc));
        SeedLease(db, DateTime.Today.AddMonths(-6));
        db.RentPayments.AddRange(
            new RentPayment { LeaseId = 1, Amount = 1000, PaymentDate = DateTime.Today.AddMonths(-2) },
            new RentPayment { LeaseId = 1, Amount = 1000, PaymentDate = DateTime.Today.AddMonths(-1) }
        );
        db.SaveChanges();

        var result = await new RentPaymentService(db).GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.True(result[0].PaymentDate >= result[1].PaymentDate);
    }
}
