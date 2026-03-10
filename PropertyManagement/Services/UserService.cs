using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;
using System.Security.Cryptography;
using System.Text;

namespace PropertyManagement.Services;

public interface IUserService
{
    Task<ApplicationUser?> ValidateCredentialsAsync(string email, string password);
    Task<List<ApplicationUser>> GetAllAsync();
    Task<ApplicationUser?> GetByIdAsync(int id);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    Task<ApplicationUser> CreateAsync(ApplicationUser user, string password);
    Task<ApplicationUser> UpdateAsync(ApplicationUser user, string? newPassword = null);
    Task DeactivateAsync(int id);
    Task UpdateLastLoginAsync(int id);
    Task<List<ApplicationUser>> GetByRoleAsync(UserRole role);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;

    public UserService(ApplicationDbContext db) => _db = db;

    public async Task<ApplicationUser?> ValidateCredentialsAsync(string email, string password)
    {
        var hash = HashPassword(password);
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email == email && u.PasswordHash == hash && u.IsActive);
        return user;
    }

    public Task<List<ApplicationUser>> GetAllAsync() =>
        _db.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();

    public Task<ApplicationUser?> GetByIdAsync(int id) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<bool> EmailExistsAsync(string email, int? excludeId = null) =>
        _db.Users.AnyAsync(u => u.Email == email && (excludeId == null || u.Id != excludeId));

    public async Task<ApplicationUser> CreateAsync(ApplicationUser user, string password)
    {
        user.PasswordHash = HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<ApplicationUser> UpdateAsync(ApplicationUser user, string? newPassword = null)
    {
        if (!string.IsNullOrWhiteSpace(newPassword))
            user.PasswordHash = HashPassword(newPassword);
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task DeactivateAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null)
        {
            user.IsActive = false;
            await _db.SaveChangesAsync();
        }
    }

    public async Task UpdateLastLoginAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null)
        {
            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public Task<List<ApplicationUser>> GetByRoleAsync(UserRole role) =>
        _db.Users.Where(u => u.Role == role && u.IsActive).OrderBy(u => u.LastName).ToListAsync();

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
