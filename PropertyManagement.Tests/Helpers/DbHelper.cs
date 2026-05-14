using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;

namespace PropertyManagement.Tests.Helpers;

public static class DbHelper
{
    public static ApplicationDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }
}
