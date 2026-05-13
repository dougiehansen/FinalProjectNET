using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class ExpenseService : IExpenseService
{
    private readonly ApplicationDbContext _db;

    public ExpenseService(ApplicationDbContext db) => _db = db;

    public async Task<List<Expense>> GetAllAsync(int? propertyId = null, ExpenseCategory? category = null, DateTime? from = null, DateTime? to = null)
    {
        var q = _db.Expenses.Include(e => e.Property).AsQueryable();

        if (propertyId > 0)  q = q.Where(e => e.PropertyId == propertyId);
        if (category != null) q = q.Where(e => e.Category == category);
        if (from != null)     q = q.Where(e => e.Date >= from.Value);
        if (to != null)       q = q.Where(e => e.Date <= to.Value);

        return await q.OrderByDescending(e => e.Date).ToListAsync();
    }

    public Task<Expense?> GetByIdAsync(int id) =>
        _db.Expenses.Include(e => e.Property).FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Expense> CreateAsync(Expense expense)
    {
        expense.CreatedAt = DateTime.UtcNow;
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        return expense;
    }

    public async Task UpdateAsync(Expense expense)
    {
        _db.Expenses.Update(expense);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.Expenses.FindAsync(id);
        if (e != null)
        {
            _db.Expenses.Remove(e);
            await _db.SaveChangesAsync();
        }
    }
}
