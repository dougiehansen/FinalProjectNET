using PropertyManagement.Models;
using PropertyManagement.Services;
using PropertyManagement.Tests.Helpers;

namespace PropertyManagement.Tests.Services;

public class ExpenseServiceTests
{
    static Property SeedProperty(PropertyManagement.Data.ApplicationDbContext db, int id = 1)
    {
        var p = new Property { Id = id, Name = $"Prop{id}", Address = "1 St", City = "C", IsActive = true };
        db.Properties.Add(p);
        db.SaveChanges();
        return p;
    }

    static Expense MakeExpense(int propId, decimal amount, ExpenseCategory cat, DateTime date, string desc = "Test") =>
        new() { PropertyId = propId, Amount = amount, Category = cat, Date = date, Description = desc };

    // ── GetAllAsync filtering ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAll_WhenNoFilters()
    {
        using var db = DbHelper.CreateDb(nameof(GetAllAsync_ReturnsAll_WhenNoFilters));
        SeedProperty(db);
        db.Expenses.AddRange(
            MakeExpense(1, 100, ExpenseCategory.Maintenance, DateTime.Today),
            MakeExpense(1, 200, ExpenseCategory.Utilities,   DateTime.Today.AddDays(-1))
        );
        db.SaveChanges();

        var result = await new ExpenseService(db).GetAllAsync();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByPropertyId()
    {
        using var db = DbHelper.CreateDb(nameof(GetAllAsync_FiltersByPropertyId));
        SeedProperty(db, 1);
        SeedProperty(db, 2);
        db.Expenses.AddRange(
            MakeExpense(1, 100, ExpenseCategory.Maintenance, DateTime.Today),
            MakeExpense(2, 200, ExpenseCategory.Maintenance, DateTime.Today)
        );
        db.SaveChanges();

        var result = await new ExpenseService(db).GetAllAsync(propertyId: 1);
        Assert.Single(result);
        Assert.Equal(1, result[0].PropertyId);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCategory()
    {
        using var db = DbHelper.CreateDb(nameof(GetAllAsync_FiltersByCategory));
        SeedProperty(db);
        db.Expenses.AddRange(
            MakeExpense(1, 100, ExpenseCategory.Maintenance, DateTime.Today),
            MakeExpense(1, 200, ExpenseCategory.Utilities,   DateTime.Today)
        );
        db.SaveChanges();

        var result = await new ExpenseService(db).GetAllAsync(category: ExpenseCategory.Maintenance);
        Assert.Single(result);
        Assert.Equal(ExpenseCategory.Maintenance, result[0].Category);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByDateRange()
    {
        using var db  = DbHelper.CreateDb(nameof(GetAllAsync_FiltersByDateRange));
        SeedProperty(db);
        var jan = new DateTime(2026, 1, 15);
        var mar = new DateTime(2026, 3, 15);
        db.Expenses.AddRange(
            MakeExpense(1, 100, ExpenseCategory.Repairs, jan),
            MakeExpense(1, 200, ExpenseCategory.Repairs, mar)
        );
        db.SaveChanges();

        var result = await new ExpenseService(db).GetAllAsync(
            from: new DateTime(2026, 1, 1),
            to:   new DateTime(2026, 2, 1));

        Assert.Single(result);
        Assert.Equal(jan, result[0].Date);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByDateDescending()
    {
        using var db = DbHelper.CreateDb(nameof(GetAllAsync_OrdersByDateDescending));
        SeedProperty(db);
        db.Expenses.AddRange(
            MakeExpense(1, 100, ExpenseCategory.Cleaning, new DateTime(2026, 1, 1)),
            MakeExpense(1, 200, ExpenseCategory.Cleaning, new DateTime(2026, 6, 1))
        );
        db.SaveChanges();

        var result = await new ExpenseService(db).GetAllAsync();
        Assert.Equal(new DateTime(2026, 6, 1), result[0].Date);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsExpense_WhenFound()
    {
        using var db = DbHelper.CreateDb(nameof(GetByIdAsync_ReturnsExpense_WhenFound));
        SeedProperty(db);
        var e = MakeExpense(1, 500, ExpenseCategory.Insurance, DateTime.Today);
        db.Expenses.Add(e);
        db.SaveChanges();

        var result = await new ExpenseService(db).GetByIdAsync(e.Id);
        Assert.NotNull(result);
        Assert.Equal(500, result!.Amount);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = DbHelper.CreateDb(nameof(GetByIdAsync_ReturnsNull_WhenNotFound));
        var result = await new ExpenseService(db).GetByIdAsync(999);
        Assert.Null(result);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsExpenseAndSetsCreatedAt()
    {
        using var db = DbHelper.CreateDb(nameof(CreateAsync_PersistsExpenseAndSetsCreatedAt));
        SeedProperty(db);
        var e = MakeExpense(1, 350, ExpenseCategory.Taxes, DateTime.Today);

        await new ExpenseService(db).CreateAsync(e);

        var saved = db.Expenses.Single();
        Assert.Equal(350, saved.Amount);
        Assert.True(saved.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesExpense()
    {
        using var db = DbHelper.CreateDb(nameof(UpdateAsync_UpdatesExpense));
        SeedProperty(db);
        var e = MakeExpense(1, 100, ExpenseCategory.Legal, DateTime.Today);
        db.Expenses.Add(e);
        db.SaveChanges();

        e.Amount = 999;
        await new ExpenseService(db).UpdateAsync(e);

        Assert.Equal(999, db.Expenses.Find(e.Id)!.Amount);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesExpense()
    {
        using var db = DbHelper.CreateDb(nameof(DeleteAsync_RemovesExpense));
        SeedProperty(db);
        var e = MakeExpense(1, 100, ExpenseCategory.Other, DateTime.Today);
        db.Expenses.Add(e);
        db.SaveChanges();

        await new ExpenseService(db).DeleteAsync(e.Id);

        Assert.Empty(db.Expenses);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotThrow_WhenIdNotFound()
    {
        using var db = DbHelper.CreateDb(nameof(DeleteAsync_DoesNotThrow_WhenIdNotFound));
        var ex = await Record.ExceptionAsync(() => new ExpenseService(db).DeleteAsync(999));
        Assert.Null(ex);
    }
}
