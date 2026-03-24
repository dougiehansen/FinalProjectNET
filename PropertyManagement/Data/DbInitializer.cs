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
            new User { FirstName = "Admin",       LastName = "User",    Email = "admin@property.com",       Role = UserRole.Administrator },
            new User { FirstName = "Code",        LastName = "Testing", Email = "codetestingtu@gmail.com",   Role = UserRole.Administrator },
            new User { FirstName = "Property",    LastName = "Manager", Email = "manager@property.com",     Role = UserRole.PropertyManager },
            new User { FirstName = "Maintenance", LastName = "Staff",   Email = "maintenance@property.com", Role = UserRole.MaintenanceStaff },
            new User { FirstName = "Accounting",  LastName = "Staff",   Email = "accounting@property.com",  Role = UserRole.AccountingTeam },
        };
        string[] passwords = { "Admin123!", "Admin123!", "Manager123!", "Maint123!", "Acct123!" };

        for (int i = 0; i < users.Length; i++)
            users[i].PasswordHash = hasher.HashPassword(users[i], passwords[i]);

        db.Users.AddRange(users);
        await db.SaveChangesAsync();
    }
}
