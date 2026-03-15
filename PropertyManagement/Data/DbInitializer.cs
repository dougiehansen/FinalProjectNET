using Microsoft.AspNetCore.Identity;
using PropertyManagement.Models;

namespace PropertyManagement.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (db.Users.Any()) return;

        var hasher = new PasswordHasher<User>();
        var users = new[]
        {
            new User { FullName = "Admin User",        Email = "admin@property.com",       Role = UserRole.Admin },
            new User { FullName = "Property Manager",  Email = "manager@property.com",     Role = UserRole.Manager },
            new User { FullName = "Maintenance Staff", Email = "maintenance@property.com", Role = UserRole.Staff },
            new User { FullName = "Accounting Staff",  Email = "accounting@property.com",  Role = UserRole.Staff },
        };
        string[] passwords = { "Admin123!", "Manager123!", "Maint123!", "Acct123!" };

        for (int i = 0; i < users.Length; i++)
            users[i].PasswordHash = hasher.HashPassword(users[i], passwords[i]);

        db.Users.AddRange(users);
        await db.SaveChangesAsync();
    }
}
