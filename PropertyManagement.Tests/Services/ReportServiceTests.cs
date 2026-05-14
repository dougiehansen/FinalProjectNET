using PropertyManagement.Models;
using PropertyManagement.Services;
using PropertyManagement.Tests.Helpers;

namespace PropertyManagement.Tests.Services;

public class ReportServiceTests
{
    // ── seed helpers ──────────────────────────────────────────────────────────

    static (Property prop, Unit unit, Tenant tenant) SeedOccupied(
        PropertyManagement.Data.ApplicationDbContext db,
        int propId, string propName, DateTime start, DateTime end)
    {
        var prop   = new Property { Id = propId, Name = propName, Address = "1 St", City = "City", IsActive = true };
        var unit   = new Unit     { Id = propId * 10, PropertyId = propId, UnitNumber = "A1", MonthlyRent = 1000, IsActive = true };
        var tenant = new Tenant   { Id = propId * 10, FirstName = "Alice", LastName = "Smith", Email = "a@b.com" };
        var lease  = new Lease    { Id = propId * 10, UnitId = unit.Id, TenantId = tenant.Id, StartDate = start, EndDate = end, MonthlyRent = 1000, Status = LeaseStatus.Active };

        db.Properties.Add(prop);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.SaveChanges();

        return (prop, unit, tenant);
    }

    // ── GetOccupancySummaryAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetOccupancySummary_AllProperties_WhenPropertyIdZero()
    {
        using var db = DbHelper.CreateDb(nameof(GetOccupancySummary_AllProperties_WhenPropertyIdZero));
        var today = DateTime.Today;
        SeedOccupied(db, 1, "Alpha", today.AddDays(-30), today.AddDays(335));
        SeedOccupied(db, 2, "Beta",  today.AddDays(-30), today.AddDays(335));

        var svc  = new ReportService(db);
        var rows = await svc.GetOccupancySummaryAsync(0);

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public async Task GetOccupancySummary_FiltersByPropertyId()
    {
        using var db = DbHelper.CreateDb(nameof(GetOccupancySummary_FiltersByPropertyId));
        var today = DateTime.Today;
        SeedOccupied(db, 1, "Alpha", today.AddDays(-30), today.AddDays(335));
        SeedOccupied(db, 2, "Beta",  today.AddDays(-30), today.AddDays(335));

        var svc  = new ReportService(db);
        var rows = await svc.GetOccupancySummaryAsync(1);

        Assert.Single(rows);
        Assert.Equal("Alpha", rows[0].PropertyName);
    }

    [Fact]
    public async Task GetOccupancySummary_CalculatesOccupiedAndVacant()
    {
        using var db = DbHelper.CreateDb(nameof(GetOccupancySummary_CalculatesOccupiedAndVacant));
        var today = DateTime.Today;
        var prop  = new Property { Id = 1, Name = "Prop", Address = "1 St", City = "C", IsActive = true };
        var u1    = new Unit { Id = 1, PropertyId = 1, UnitNumber = "1", MonthlyRent = 900,  IsActive = true };
        var u2    = new Unit { Id = 2, PropertyId = 1, UnitNumber = "2", MonthlyRent = 1100, IsActive = true };
        var t1    = new Tenant { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com" };
        var l1    = new Lease  { Id = 1, UnitId = 1, TenantId = 1, StartDate = today.AddDays(-10), EndDate = today.AddDays(355), MonthlyRent = 900, Status = LeaseStatus.Active };

        db.Properties.Add(prop);
        db.Units.AddRange(u1, u2);
        db.Tenants.Add(t1);
        db.Leases.Add(l1);
        db.SaveChanges();

        var svc  = new ReportService(db);
        var rows = await svc.GetOccupancySummaryAsync(0);

        Assert.Single(rows);
        Assert.Equal(2, rows[0].TotalUnits);
        Assert.Equal(1, rows[0].OccupiedUnits);
        Assert.Equal(1, rows[0].VacantUnits);
        Assert.Equal(50, rows[0].OccupancyRate);
    }

    [Fact]
    public async Task GetOccupancySummary_ReturnsEmpty_WhenNoProperties()
    {
        using var db  = DbHelper.CreateDb(nameof(GetOccupancySummary_ReturnsEmpty_WhenNoProperties));
        var svc  = new ReportService(db);
        var rows = await svc.GetOccupancySummaryAsync(0);
        Assert.Empty(rows);
    }

    // ── GetRentRollAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetRentRoll_ReturnsActiveLeases_AsOf()
    {
        using var db = DbHelper.CreateDb(nameof(GetRentRoll_ReturnsActiveLeases_AsOf));
        var today = DateTime.Today;
        SeedOccupied(db, 1, "Prop", today.AddDays(-30), today.AddDays(335));

        var svc  = new ReportService(db);
        var rows = await svc.GetRentRollAsync(0, today);

        Assert.Single(rows);
        Assert.Equal("A1", rows[0].UnitNumber);
    }

    [Fact]
    public async Task GetRentRoll_ExcludesLeases_NotYetStarted()
    {
        using var db = DbHelper.CreateDb(nameof(GetRentRoll_ExcludesLeases_NotYetStarted));
        var today = DateTime.Today;
        SeedOccupied(db, 1, "Prop", today.AddDays(10), today.AddDays(375));

        var svc  = new ReportService(db);
        var rows = await svc.GetRentRollAsync(0, today);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task GetRentRoll_FiltersByPropertyId()
    {
        using var db = DbHelper.CreateDb(nameof(GetRentRoll_FiltersByPropertyId));
        var today = DateTime.Today;
        SeedOccupied(db, 1, "Alpha", today.AddDays(-10), today.AddDays(355));
        SeedOccupied(db, 2, "Beta",  today.AddDays(-10), today.AddDays(355));

        var svc  = new ReportService(db);
        var rows = await svc.GetRentRollAsync(2, today);

        Assert.Single(rows);
        Assert.Equal("Beta", rows[0].PropertyName);
    }

    [Fact]
    public async Task GetRentRoll_RowHasCorrectTenantName()
    {
        using var db = DbHelper.CreateDb(nameof(GetRentRoll_RowHasCorrectTenantName));
        var today = DateTime.Today;
        SeedOccupied(db, 1, "Prop", today.AddDays(-10), today.AddDays(355));

        var svc  = new ReportService(db);
        var rows = await svc.GetRentRollAsync(0, today);

        Assert.Equal("Alice Smith", rows[0].TenantName);
    }

    // ── GetOutstandingPaymentsAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetOutstandingPayments_ReturnsTenants_WithBalance()
    {
        using var db = DbHelper.CreateDb(nameof(GetOutstandingPayments_ReturnsTenants_WithBalance));
        var today = DateTime.Today;
        SeedOccupied(db, 1, "Prop", today.AddMonths(-3), today.AddMonths(9));

        var svc  = new ReportService(db);
        var rows = await svc.GetOutstandingPaymentsAsync(0, today);

        Assert.Single(rows);
        Assert.True(rows[0].OutstandingBalance > 0);
    }

    [Fact]
    public async Task GetOutstandingPayments_ExcludesPaidUpTenants()
    {
        using var db = DbHelper.CreateDb(nameof(GetOutstandingPayments_ExcludesPaidUpTenants));
        var today = DateTime.Today;
        var (_, unit, tenant) = SeedOccupied(db, 1, "Prop", today.AddMonths(-2), today.AddMonths(10));
        var lease = db.Leases.First();

        // pay off all expected rent
        var monthsActive = (int)Math.Max(1, Math.Floor((today - lease.StartDate).TotalDays / 30.44));
        db.RentPayments.Add(new RentPayment
        {
            LeaseId = lease.Id,
            Amount  = monthsActive * lease.MonthlyRent,
            PaymentDate = today
        });
        db.SaveChanges();

        var svc  = new ReportService(db);
        var rows = await svc.GetOutstandingPaymentsAsync(0, today);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task GetOutstandingPayments_FiltersByPropertyId()
    {
        using var db = DbHelper.CreateDb(nameof(GetOutstandingPayments_FiltersByPropertyId));
        var today = DateTime.Today;
        SeedOccupied(db, 1, "Alpha", today.AddMonths(-3), today.AddMonths(9));
        SeedOccupied(db, 2, "Beta",  today.AddMonths(-3), today.AddMonths(9));

        var svc  = new ReportService(db);
        var rows = await svc.GetOutstandingPaymentsAsync(1, today);

        Assert.Single(rows);
        Assert.Equal("Alpha", rows[0].PropertyName);
    }

    // ── GetProfitLossAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfitLoss_CalculatesNetIncome()
    {
        using var db = DbHelper.CreateDb(nameof(GetProfitLoss_CalculatesNetIncome));
        var from  = new DateTime(2026, 1, 1);
        var to    = new DateTime(2026, 1, 31);
        var (prop, unit, tenant) = SeedOccupied(db, 1, "Prop", from.AddDays(-10), to.AddDays(300));
        var lease = db.Leases.First();

        db.RentPayments.Add(new RentPayment { LeaseId = lease.Id, Amount = 1200, PaymentDate = new DateTime(2026, 1, 15) });
        db.Expenses.Add(new Expense { PropertyId = prop.Id, Amount = 300, Date = new DateTime(2026, 1, 10), Category = ExpenseCategory.Maintenance, Description = "Fix" });
        db.SaveChanges();

        var svc  = new ReportService(db);
        var rows = await svc.GetProfitLossAsync(0, from, to);

        Assert.Single(rows);
        Assert.Equal(1200m, rows[0].RentRevenue);
        Assert.Equal(300m,  rows[0].TotalExpenses);
        Assert.Equal(900m,  rows[0].NetIncome);
    }

    [Fact]
    public async Task GetProfitLoss_GroupsByProperty()
    {
        using var db = DbHelper.CreateDb(nameof(GetProfitLoss_GroupsByProperty));
        var from  = new DateTime(2026, 1, 1);
        var to    = new DateTime(2026, 1, 31);

        var p1 = new Property { Id = 1, Name = "Alpha", Address = "1 St", City = "C", IsActive = true };
        var p2 = new Property { Id = 2, Name = "Beta",  Address = "2 St", City = "C", IsActive = true };
        var u1 = new Unit { Id = 1, PropertyId = 1, UnitNumber = "1", MonthlyRent = 1000, IsActive = true };
        var u2 = new Unit { Id = 2, PropertyId = 2, UnitNumber = "2", MonthlyRent = 1000, IsActive = true };
        var t1 = new Tenant { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com" };
        var t2 = new Tenant { Id = 2, FirstName = "C", LastName = "D", Email = "c@d.com" };
        var l1 = new Lease { Id = 1, UnitId = 1, TenantId = 1, StartDate = from.AddDays(-10), EndDate = to.AddDays(300), MonthlyRent = 1000, Status = LeaseStatus.Active };
        var l2 = new Lease { Id = 2, UnitId = 2, TenantId = 2, StartDate = from.AddDays(-10), EndDate = to.AddDays(300), MonthlyRent = 1000, Status = LeaseStatus.Active };

        db.Properties.AddRange(p1, p2);
        db.Units.AddRange(u1, u2);
        db.Tenants.AddRange(t1, t2);
        db.Leases.AddRange(l1, l2);
        db.RentPayments.Add(new RentPayment { LeaseId = 1, Amount = 500, PaymentDate = new DateTime(2026, 1, 5) });
        db.RentPayments.Add(new RentPayment { LeaseId = 2, Amount = 800, PaymentDate = new DateTime(2026, 1, 5) });
        db.SaveChanges();

        var svc  = new ReportService(db);
        var rows = await svc.GetProfitLossAsync(0, from, to);

        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.PropertyName == "Alpha" && r.RentRevenue == 500);
        Assert.Contains(rows, r => r.PropertyName == "Beta"  && r.RentRevenue == 800);
    }

    [Fact]
    public async Task GetProfitLoss_FiltersByPropertyId()
    {
        using var db = DbHelper.CreateDb(nameof(GetProfitLoss_FiltersByPropertyId));
        var from = new DateTime(2026, 1, 1);
        var to   = new DateTime(2026, 1, 31);
        var (p1, _, _) = SeedOccupied(db, 1, "Alpha", from.AddDays(-10), to.AddDays(300));
        SeedOccupied(db, 2, "Beta", from.AddDays(-10), to.AddDays(300));

        var l1 = db.Leases.First(l => l.Unit.PropertyId == 1);
        db.RentPayments.Add(new RentPayment { LeaseId = l1.Id, Amount = 600, PaymentDate = new DateTime(2026, 1, 10) });
        db.SaveChanges();

        var svc  = new ReportService(db);
        var rows = await svc.GetProfitLossAsync(1, from, to);

        Assert.Single(rows);
        Assert.Equal("Alpha", rows[0].PropertyName);
    }

    [Fact]
    public async Task GetProfitLoss_ExpensesByCategory_AreGrouped()
    {
        using var db = DbHelper.CreateDb(nameof(GetProfitLoss_ExpensesByCategory_AreGrouped));
        var from = new DateTime(2026, 1, 1);
        var to   = new DateTime(2026, 1, 31);
        var (prop, _, _) = SeedOccupied(db, 1, "Prop", from.AddDays(-10), to.AddDays(300));

        db.Expenses.AddRange(
            new Expense { PropertyId = prop.Id, Amount = 200, Date = new DateTime(2026, 1, 5),  Category = ExpenseCategory.Maintenance, Description = "A" },
            new Expense { PropertyId = prop.Id, Amount = 100, Date = new DateTime(2026, 1, 10), Category = ExpenseCategory.Maintenance, Description = "B" },
            new Expense { PropertyId = prop.Id, Amount = 150, Date = new DateTime(2026, 1, 15), Category = ExpenseCategory.Utilities,   Description = "C" }
        );
        db.SaveChanges();

        var svc  = new ReportService(db);
        var rows = await svc.GetProfitLossAsync(0, from, to);

        var row = rows[0];
        Assert.Equal(300m, row.ExpenseByCategory[ExpenseCategory.Maintenance]);
        Assert.Equal(150m, row.ExpenseByCategory[ExpenseCategory.Utilities]);
    }

    // ── ExportToExcelAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ExportToExcel_OccupancySummary_ReturnsByteArray()
    {
        using var db = DbHelper.CreateDb(nameof(ExportToExcel_OccupancySummary_ReturnsByteArray));
        SeedOccupied(db, 1, "Prop", DateTime.Today.AddDays(-30), DateTime.Today.AddDays(335));

        var svc   = new ReportService(db);
        var bytes = await svc.ExportToExcelAsync("OccupancySummary", 0, DateTime.Today);

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task ExportToExcel_RentRoll_ReturnsByteArray()
    {
        using var db = DbHelper.CreateDb(nameof(ExportToExcel_RentRoll_ReturnsByteArray));
        SeedOccupied(db, 1, "Prop", DateTime.Today.AddDays(-10), DateTime.Today.AddDays(355));

        var bytes = await new ReportService(db).ExportToExcelAsync("RentRoll", 0, DateTime.Today);

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task ExportToExcel_OutstandingPayments_ReturnsByteArray()
    {
        using var db = DbHelper.CreateDb(nameof(ExportToExcel_OutstandingPayments_ReturnsByteArray));
        SeedOccupied(db, 1, "Prop", DateTime.Today.AddMonths(-3), DateTime.Today.AddMonths(9));

        var bytes = await new ReportService(db).ExportToExcelAsync("OutstandingPayments", 0, DateTime.Today);

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task ExportToExcel_ProfitLoss_ReturnsByteArray()
    {
        using var db  = DbHelper.CreateDb(nameof(ExportToExcel_ProfitLoss_ReturnsByteArray));
        var from = new DateTime(2026, 1, 1);
        var to   = new DateTime(2026, 1, 31);
        var (prop, _, _) = SeedOccupied(db, 1, "Prop", from.AddDays(-10), to.AddDays(300));
        var lease = db.Leases.First();
        db.RentPayments.Add(new RentPayment { LeaseId = lease.Id, Amount = 1000, PaymentDate = new DateTime(2026, 1, 15) });
        db.SaveChanges();

        var bytes = await new ReportService(db).ExportToExcelAsync("ProfitLoss", 0, plFrom: from, plTo: to);

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task ExportToExcel_ProfitLoss_NegativeNetIncome_StillExports()
    {
        using var db  = DbHelper.CreateDb(nameof(ExportToExcel_ProfitLoss_NegativeNetIncome_StillExports));
        var from = new DateTime(2026, 1, 1);
        var to   = new DateTime(2026, 1, 31);
        var (prop, _, _) = SeedOccupied(db, 1, "Prop", from.AddDays(-10), to.AddDays(300));
        db.Expenses.Add(new Expense { PropertyId = prop.Id, Amount = 9999, Date = new DateTime(2026, 1, 5), Category = ExpenseCategory.Repairs, Description = "Big repair" });
        db.SaveChanges();

        var bytes = await new ReportService(db).ExportToExcelAsync("ProfitLoss", 0, plFrom: from, plTo: to);

        Assert.NotEmpty(bytes);
    }

}
