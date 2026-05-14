using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;

namespace PropertyManagement.Tests.Helpers;

public static class DbHelper
{
    public static ApplicationDbContext CreateDb(string name = "")
    {
        var dbName = string.IsNullOrEmpty(name)
            ? Guid.NewGuid().ToString()
            : $"{name}_{Guid.NewGuid():N}";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }
}
