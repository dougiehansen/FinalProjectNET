using PropertyManagement.Models;
using PropertyManagement.Services;
using PropertyManagement.Tests.Helpers;

namespace PropertyManagement.Tests.Services;

public class TenantServiceTests
{
    static Tenant MakeTenant(int id, string first = "Alice", string last = "Smith") =>
        new() { Id = id, FirstName = first, LastName = last, Email = $"{first}@test.com", IsActive = true };

    static Lease MakeLease(int id, int tenantId, int unitId, DateTime start, DateTime end,
        decimal rent = 1000, LeaseStatus status = LeaseStatus.Active) =>
        new() { Id = id, TenantId = tenantId, UnitId = unitId, StartDate = start, EndDate = end, MonthlyRent = rent, Status = status };

    // ── Health scores ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHealthScores_Green_WhenPaidAndNotExpiring()
    {
        using var db  = DbHelper.CreateDb(nameof(GetHealthScores_Green_WhenPaidAndNotExpiring));
        var today     = DateTime.Today;
        var tenant    = MakeTenant(1);
        var lease     = MakeLease(1, 1, 1, today.AddDays(-30), today.AddDays(365));
        var monthsPaid = (int)Math.Max(1, Math.Floor(30.0 / 30.44));
        lease.RentPayments.Add(new RentPayment { Amount = monthsPaid * 1000m, PaymentDate = today });

        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.SaveChanges();

        var svc    = new TenantService(db);
        var scores = await svc.GetHealthScoresAsync([1]);

        Assert.Equal("Green", scores[1].Level);
    }

    [Fact]
    public async Task GetHealthScores_Red_WhenBalanceOwed()
    {
        using var db = DbHelper.CreateDb(nameof(GetHealthScores_Red_WhenBalanceOwed));
        var today    = DateTime.Today;
        var tenant   = MakeTenant(1);
        var lease    = MakeLease(1, 1, 1, today.AddMonths(-3), today.AddMonths(9));

        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.SaveChanges();

        var svc    = new TenantService(db);
        var scores = await svc.GetHealthScoresAsync([1]);

        Assert.Equal("Red", scores[1].Level);
        Assert.Equal("Overdue", scores[1].Label);
    }

    [Fact]
    public async Task GetHealthScores_Amber_WhenNoActiveLease()
    {
        using var db = DbHelper.CreateDb(nameof(GetHealthScores_Amber_WhenNoActiveLease));
        var tenant   = MakeTenant(1);
        db.Tenants.Add(tenant);
        db.SaveChanges();

        var svc    = new TenantService(db);
        var scores = await svc.GetHealthScoresAsync([1]);

        Assert.Equal("Amber", scores[1].Level);
        Assert.Equal("No lease", scores[1].Label);
    }

    [Fact]
    public async Task GetHealthScores_Amber_WhenLeaseExpiringSoon()
    {
        using var db = DbHelper.CreateDb(nameof(GetHealthScores_Amber_WhenLeaseExpiringSoon));
        var today    = DateTime.Today;
        var tenant   = MakeTenant(1);
        var lease    = MakeLease(1, 1, 1, today.AddDays(-30), today.AddDays(30));
        var months   = (int)Math.Max(1, Math.Floor(30.0 / 30.44));
        lease.RentPayments.Add(new RentPayment { Amount = months * 1000m, PaymentDate = today });

        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.SaveChanges();

        var svc    = new TenantService(db);
        var scores = await svc.GetHealthScoresAsync([1]);

        Assert.Equal("Amber", scores[1].Level);
        Assert.Equal("Expiring soon", scores[1].Label);
    }

    [Fact]
    public async Task GetHealthScores_MultiTenant_ReturnsAllScores()
    {
        using var db = DbHelper.CreateDb(nameof(GetHealthScores_MultiTenant_ReturnsAllScores));
        var today    = DateTime.Today;
        var t1 = MakeTenant(1); // no lease → Amber
        var t2 = MakeTenant(2); // overdue → Red
        var lease2 = MakeLease(2, 2, 2, today.AddMonths(-2), today.AddMonths(10));

        db.Tenants.AddRange(t1, t2);
        db.Leases.Add(lease2);
        db.SaveChanges();

        var svc    = new TenantService(db);
        var scores = await svc.GetHealthScoresAsync([1, 2]);

        Assert.Equal("Amber", scores[1].Level);
        Assert.Equal("Red",   scores[2].Level);
    }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_SetsCreatedAtAndPersists()
    {
        using var db = DbHelper.CreateDb(nameof(CreateAsync_SetsCreatedAtAndPersists));
        var svc     = new TenantService(db);
        var tenant  = new Tenant { FirstName = "Bob", LastName = "Jones", Email = "bob@test.com" };

        await svc.CreateAsync(tenant);

        var saved = db.Tenants.Single();
        Assert.Equal("Bob", saved.FirstName);
        Assert.True(saved.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllFields()
    {
        using var db = DbHelper.CreateDb(nameof(UpdateAsync_UpdatesAllFields));
        var tenant   = MakeTenant(1);
        db.Tenants.Add(tenant);
        db.SaveChanges();

        var svc = new TenantService(db);
        await svc.UpdateAsync(new Tenant
        {
            Id = 1, FirstName = "Updated", LastName = "Name", Email = "new@test.com",
            Phone = "0871234567", EmployerName = "ACME", Notes = "VIP"
        });

        var saved = db.Tenants.Find(1)!;
        Assert.Equal("Updated", saved.FirstName);
        Assert.Equal("new@test.com", saved.Email);
        Assert.Equal("ACME", saved.EmployerName);
        Assert.Equal("VIP", saved.Notes);
    }

    [Fact]
    public async Task UpdateAsync_DoesNothing_WhenTenantNotFound()
    {
        using var db = DbHelper.CreateDb(nameof(UpdateAsync_DoesNothing_WhenTenantNotFound));
        var svc  = new TenantService(db);
        await svc.UpdateAsync(new Tenant { Id = 999, FirstName = "Ghost", LastName = "X", Email = "g@x.com" });
        Assert.Empty(db.Tenants);
    }

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveFalse()
    {
        using var db = DbHelper.CreateDb(nameof(DeactivateAsync_SetsIsActiveFalse));
        var tenant   = MakeTenant(1);
        db.Tenants.Add(tenant);
        db.SaveChanges();

        await new TenantService(db).DeactivateAsync(1);

        Assert.False(db.Tenants.Find(1)!.IsActive);
    }

    [Fact]
    public async Task ActivateAsync_SetsIsActiveTrue()
    {
        using var db = DbHelper.CreateDb(nameof(ActivateAsync_SetsIsActiveTrue));
        var tenant   = MakeTenant(1);
        tenant.IsActive = false;
        db.Tenants.Add(tenant);
        db.SaveChanges();

        await new TenantService(db).ActivateAsync(1);

        Assert.True(db.Tenants.Find(1)!.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTenants_OrderedByLastName()
    {
        using var db = DbHelper.CreateDb(nameof(GetAllAsync_ReturnsTenants_OrderedByLastName));
        db.Tenants.AddRange(MakeTenant(1, "B", "Zebra"), MakeTenant(2, "A", "Apple"));
        db.SaveChanges();

        var result = await new TenantService(db).GetAllAsync();

        Assert.Equal("Apple", result[0].LastName);
        Assert.Equal("Zebra", result[1].LastName);
    }
}
