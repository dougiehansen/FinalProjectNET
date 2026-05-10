using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();

    public UserService(ApplicationDbContext db) => _db = db;

    public async Task<User?> GetByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

    public async Task<User?> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (user == null) return null;
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<User>> GetAllAsync() =>
        await _db.Users.OrderBy(u => u.LastName).ToListAsync();

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null) =>
        await _db.Users.AnyAsync(u => u.Email == email && (excludeId == null || u.Id != excludeId));

    public async Task CreateAsync(User user, string password)
    {
        user.PasswordHash = _hasher.HashPassword(user, password);
        user.CreatedAt = DateTime.UtcNow;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user, string? newPassword = null)
    {
        var existing = await _db.Users.FindAsync(user.Id);
        if (existing == null) return;

        existing.FirstName = user.FirstName;
        existing.LastName = user.LastName;
        existing.Email = user.Email;
        existing.Role = user.Role;
        existing.IsActive = user.IsActive;

        if (!string.IsNullOrWhiteSpace(newPassword))
            existing.PasswordHash = _hasher.HashPassword(existing, newPassword);

        await _db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsActive = false;
            await _db.SaveChangesAsync();
        }
    }

    public async Task ActivateAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsActive = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<User>> GetByRoleAsync(UserRole role) =>
        await _db.Users
            .Where(u => u.Role == role && u.IsActive)
            .OrderBy(u => u.LastName)
            .ToListAsync();

    public async Task<List<int>> GetAssignedPropertyIdsAsync(int userId) =>
        await _db.UserPropertyAssignments
            .Where(a => a.UserId == userId)
            .Select(a => a.PropertyId)
            .ToListAsync();

    public async Task SetPropertyAssignmentsAsync(int userId, IEnumerable<int> propertyIds)
    {
        var existing = _db.UserPropertyAssignments.Where(a => a.UserId == userId);
        _db.UserPropertyAssignments.RemoveRange(existing);
        foreach (var pid in propertyIds)
            _db.UserPropertyAssignments.Add(new Models.UserPropertyAssignment { UserId = userId, PropertyId = pid });
        await _db.SaveChangesAsync();
    }

    public async Task<Dictionary<int, int>> GetAssignmentCountsAsync() =>
        (await _db.UserPropertyAssignments.ToListAsync())
            .GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.Count());
}
