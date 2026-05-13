using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetAllAsync(int? propertyId = null, ExpenseCategory? category = null, DateTime? from = null, DateTime? to = null);
    Task<Expense?>      GetByIdAsync(int id);
    Task<Expense>       CreateAsync(Expense expense);
    Task                UpdateAsync(Expense expense);
    Task                DeleteAsync(int id);
}
