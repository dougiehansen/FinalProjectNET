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
}
