using PropertyManagement.Models;
using PropertyManagement.Services;
using PropertyManagement.Tests.Helpers;

namespace PropertyManagement.Tests.Services;

public class LeaseServiceTests
{
    static (Property, Unit, Tenant) SeedBase(PropertyManagement.Data.ApplicationDbContext db, int id = 1)
    {
        var prop   = new Property { Id = id, Name = $"P{id}", Address = "1 St", City = "C", IsActive = true };
        var unit   = new Unit     { Id = id, PropertyId = id, UnitNumber = "A", MonthlyRent = 1000, IsActive = true };
        var tenant = new Tenant   { Id = id, FirstName = "Tom", LastName = "Lee", Email = $"t{id}@x.com" };
        db.Properties.Add(prop);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.SaveChanges();
        return (prop, unit, tenant);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_SetsUnitOccupied_WhenActive()
    {
        using var db     = DbHelper.CreateDb(nameof(CreateAsync_SetsUnitOccupied_WhenActive));
        var (_, unit, _) = SeedBase(db);

        var lease = new Lease { TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), MonthlyRent = 1000, Status = LeaseStatus.Active };
        await new LeaseService(db).CreateAsync(lease);

        Assert.True(db.Units.Find(1)!.IsOccupied);
    }

    [Fact]
    public async Task CreateAsync_DoesNotSetOccupied_WhenExpired()
    {
        using var db     = DbHelper.CreateDb(nameof(CreateAsync_DoesNotSetOccupied_WhenExpired));
        var (_, unit, _) = SeedBase(db);

        var lease = new Lease { TenantId = 1, UnitId = 1, StartDate = DateTime.Today.AddYears(-2), EndDate = DateTime.Today.AddYears(-1), MonthlyRent = 1000, Status = LeaseStatus.Expired };
        await new LeaseService(db).CreateAsync(lease);

        Assert.False(db.Units.Find(1)!.IsOccupied);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsAndSyncsOccupancy()
    {
        using var db     = DbHelper.CreateDb(nameof(UpdateAsync_UpdatesFieldsAndSyncsOccupancy));
        var (_, unit, _) = SeedBase(db);
        var lease = new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), MonthlyRent = 1000, Status = LeaseStatus.Active };
        db.Leases.Add(lease);
        db.SaveChanges();

        var updated = new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), MonthlyRent = 1200, Status = LeaseStatus.Expired };
        await new LeaseService(db).UpdateAsync(updated);

        var saved = db.Leases.Find(1)!;
        Assert.Equal(1200, saved.MonthlyRent);
        Assert.Equal(LeaseStatus.Expired, saved.Status);
        Assert.False(db.Units.Find(1)!.IsOccupied);
    }

    [Fact]
    public async Task UpdateAsync_DoesNothing_WhenLeaseNotFound()
    {
        using var db = DbHelper.CreateDb(nameof(UpdateAsync_DoesNothing_WhenLeaseNotFound));
        SeedBase(db);
        var svc = new LeaseService(db);
        await svc.UpdateAsync(new Lease { Id = 999, TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) });
        Assert.Empty(db.Leases);
    }

    // ── GetOutstandingBalanceAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetOutstandingBalance_ReturnsCorrectAmount()
    {
        using var db     = DbHelper.CreateDb(nameof(GetOutstandingBalance_ReturnsCorrectAmount));
        var (_, unit, _) = SeedBase(db);
        var start  = DateTime.Today.AddMonths(-2);
        var lease  = new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = start, EndDate = start.AddYears(1), MonthlyRent = 1000, Status = LeaseStatus.Active };
        db.Leases.Add(lease);
        db.SaveChanges();

        var balance = await new LeaseService(db).GetOutstandingBalanceAsync(1);

        Assert.True(balance > 0);
    }

    [Fact]
    public async Task GetOutstandingBalance_ReturnsZero_WhenFullyPaid()
    {
        using var db     = DbHelper.CreateDb(nameof(GetOutstandingBalance_ReturnsZero_WhenFullyPaid));
        var (_, unit, _) = SeedBase(db);
        var start  = DateTime.Today.AddMonths(-1);
        var lease  = new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = start, EndDate = start.AddYears(1), MonthlyRent = 1000, Status = LeaseStatus.Active };
        db.Leases.Add(lease);
        var months = (int)Math.Max(1, Math.Floor((DateTime.Today - start).TotalDays / 30.44));
        lease.RentPayments.Add(new RentPayment { Amount = months * 1000m, PaymentDate = DateTime.Today });
        db.SaveChanges();

        var balance = await new LeaseService(db).GetOutstandingBalanceAsync(1);

        Assert.Equal(0, balance);
    }

    [Fact]
    public async Task GetOutstandingBalance_ReturnsZero_WhenLeaseNotFound()
    {
        using var db = DbHelper.CreateDb(nameof(GetOutstandingBalance_ReturnsZero_WhenLeaseNotFound));
        var balance = await new LeaseService(db).GetOutstandingBalanceAsync(99);
        Assert.Equal(0, balance);
    }

    // ── AutoExpireAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AutoExpireAsync_ExpiresPastActiveLeases()
    {
        using var db     = DbHelper.CreateDb(nameof(AutoExpireAsync_ExpiresPastActiveLeases));
        var (_, unit, _) = SeedBase(db);
        var lease = new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = DateTime.Today.AddYears(-2), EndDate = DateTime.Today.AddDays(-1), MonthlyRent = 1000, Status = LeaseStatus.Active };
        db.Leases.Add(lease);
        db.SaveChanges();

        var count = await new LeaseService(db).AutoExpireAsync();

        Assert.Equal(1, count);
        Assert.Equal(LeaseStatus.Expired, db.Leases.Find(1)!.Status);
        Assert.False(db.Units.Find(1)!.IsOccupied);
    }

    [Fact]
    public async Task AutoExpireAsync_DoesNotExpire_FutureLeases()
    {
        using var db     = DbHelper.CreateDb(nameof(AutoExpireAsync_DoesNotExpire_FutureLeases));
        var (_, unit, _) = SeedBase(db);
        var lease = new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), MonthlyRent = 1000, Status = LeaseStatus.Active };
        db.Leases.Add(lease);
        db.SaveChanges();

        var count = await new LeaseService(db).AutoExpireAsync();

        Assert.Equal(0, count);
        Assert.Equal(LeaseStatus.Active, db.Leases.Find(1)!.Status);
    }

    [Fact]
    public async Task AutoExpireAsync_ReturnsZero_WhenNoLeases()
    {
        using var db = DbHelper.CreateDb(nameof(AutoExpireAsync_ReturnsZero_WhenNoLeases));
        var count = await new LeaseService(db).AutoExpireAsync();
        Assert.Equal(0, count);
    }

    // ── Signing flow ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SignAsync_SetsSignedStatusAndName()
    {
        using var db     = DbHelper.CreateDb(nameof(SignAsync_SetsSignedStatusAndName));
        var (_, unit, _) = SeedBase(db);
        var token = "abc123";
        db.Leases.Add(new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), SigningToken = token, SignatureStatus = SignatureStatus.Pending });
        db.SaveChanges();

        await new LeaseService(db).SignAsync(token, "Tom Lee");

        var lease = db.Leases.Find(1)!;
        Assert.Equal(SignatureStatus.Signed, lease.SignatureStatus);
        Assert.Equal("Tom Lee", lease.SignedByName);
        Assert.NotNull(lease.SignedAt);
    }

    [Fact]
    public async Task SignAsync_DoesNothing_WhenTokenNotFound()
    {
        using var db = DbHelper.CreateDb(nameof(SignAsync_DoesNothing_WhenTokenNotFound));
        await new LeaseService(db).SignAsync("invalid", "Nobody");
        Assert.Empty(db.Leases);
    }

    [Fact]
    public async Task DeclineAsync_SetsDeclinedStatus()
    {
        using var db     = DbHelper.CreateDb(nameof(DeclineAsync_SetsDeclinedStatus));
        var (_, unit, _) = SeedBase(db);
        var token = "tok999";
        db.Leases.Add(new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), SigningToken = token, SignatureStatus = SignatureStatus.Pending });
        db.SaveChanges();

        await new LeaseService(db).DeclineAsync(token);

        Assert.Equal(SignatureStatus.Declined, db.Leases.Find(1)!.SignatureStatus);
    }

    [Fact]
    public async Task RecordSigningOpenedAsync_StoresIpAndUserAgent()
    {
        using var db     = DbHelper.CreateDb(nameof(RecordSigningOpenedAsync_StoresIpAndUserAgent));
        var (_, unit, _) = SeedBase(db);
        var token = "openTok";
        db.Leases.Add(new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), SigningToken = token, SignatureStatus = SignatureStatus.Pending });
        db.SaveChanges();

        await new LeaseService(db).RecordSigningOpenedAsync(token, "1.2.3.4", "Mozilla/5.0");

        var lease = db.Leases.Find(1)!;
        Assert.Equal("1.2.3.4",    lease.TenantIpAddress);
        Assert.Equal("Mozilla/5.0", lease.TenantUserAgent);
        Assert.NotNull(lease.SigningPageOpenedAt);
    }

    [Fact]
    public async Task RecordSigningOpenedAsync_DoesNothing_WhenAlreadySigned()
    {
        using var db     = DbHelper.CreateDb(nameof(RecordSigningOpenedAsync_DoesNothing_WhenAlreadySigned));
        var (_, unit, _) = SeedBase(db);
        var token = "signedTok";
        db.Leases.Add(new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), SigningToken = token, SignatureStatus = SignatureStatus.Signed });
        db.SaveChanges();

        await new LeaseService(db).RecordSigningOpenedAsync(token, "9.9.9.9", "Bot");

        Assert.Null(db.Leases.Find(1)!.TenantIpAddress);
    }

    // ── UnitHasActiveLease ────────────────────────────────────────────────────

    [Fact]
    public async Task UnitHasActiveLease_ReturnsTrue_WhenActiveLeaseExists()
    {
        using var db     = DbHelper.CreateDb(nameof(UnitHasActiveLease_ReturnsTrue_WhenActiveLeaseExists));
        var (_, unit, _) = SeedBase(db);
        db.Leases.Add(new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), Status = LeaseStatus.Active });
        db.SaveChanges();

        var result = await new LeaseService(db).UnitHasActiveLease(1);
        Assert.True(result);
    }

    [Fact]
    public async Task UnitHasActiveLease_ReturnsFalse_WhenNoLease()
    {
        using var db = DbHelper.CreateDb(nameof(UnitHasActiveLease_ReturnsFalse_WhenNoLease));
        SeedBase(db);

        var result = await new LeaseService(db).UnitHasActiveLease(1);
        Assert.False(result);
    }

    [Fact]
    public async Task ConfirmReviewAsync_SetsManagerConfirmed()
    {
        using var db     = DbHelper.CreateDb(nameof(ConfirmReviewAsync_SetsManagerConfirmed));
        var (_, unit, _) = SeedBase(db);
        db.Leases.Add(new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) });
        db.SaveChanges();

        await new LeaseService(db).ConfirmReviewAsync(1);

        Assert.True(db.Leases.Find(1)!.ManagerConfirmed);
    }

    // ── GetAllAsync / GetByTenantIdAsync / GetHistoryByUnitAsync / GetActiveLeasesByUnitAsync ──

    [Fact]
    public async Task GetAllAsync_ReturnsAllLeases()
    {
        using var db     = DbHelper.CreateDb(nameof(GetAllAsync_ReturnsAllLeases));
        var (_, unit, _) = SeedBase(db);
        db.Leases.Add(new Lease { TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), MonthlyRent = 1000 });
        db.SaveChanges();

        var result = await new LeaseService(db).GetAllAsync();
        Assert.Single(result);
    }

    [Fact]
    public async Task GetByTenantIdAsync_ReturnsLeasesForTenant()
    {
        using var db     = DbHelper.CreateDb(nameof(GetByTenantIdAsync_ReturnsLeasesForTenant));
        var (_, unit, _) = SeedBase(db);
        db.Leases.Add(new Lease { TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), MonthlyRent = 1000 });
        db.SaveChanges();

        var result = await new LeaseService(db).GetByTenantIdAsync(1);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetByTenantIdAsync_ReturnsEmpty_WhenTenantHasNoLeases()
    {
        using var db = DbHelper.CreateDb(nameof(GetByTenantIdAsync_ReturnsEmpty_WhenTenantHasNoLeases));
        SeedBase(db);
        var result = await new LeaseService(db).GetByTenantIdAsync(1);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryByUnitAsync_ReturnsLeasesForUnit()
    {
        using var db     = DbHelper.CreateDb(nameof(GetHistoryByUnitAsync_ReturnsLeasesForUnit));
        var (_, unit, _) = SeedBase(db);
        db.Leases.Add(new Lease { TenantId = 1, UnitId = 1, StartDate = DateTime.Today.AddYears(-1), EndDate = DateTime.Today.AddYears(-0), MonthlyRent = 1000 });
        db.SaveChanges();

        var result = await new LeaseService(db).GetHistoryByUnitAsync(1);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetActiveLeasesByUnitAsync_ReturnsOnlyActiveLeases()
    {
        using var db     = DbHelper.CreateDb(nameof(GetActiveLeasesByUnitAsync_ReturnsOnlyActiveLeases));
        var (_, unit, _) = SeedBase(db);
        db.Leases.AddRange(
            new Lease { Id = 1, TenantId = 1, UnitId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1), Status = LeaseStatus.Active },
            new Lease { Id = 2, TenantId = 1, UnitId = 1, StartDate = DateTime.Today.AddYears(-2), EndDate = DateTime.Today.AddYears(-1), Status = LeaseStatus.Expired }
        );
        db.SaveChanges();

        var result = await new LeaseService(db).GetActiveLeasesByUnitAsync();
        Assert.Single(result);
        Assert.Equal(LeaseStatus.Active, result[1].Status);
    }
}
