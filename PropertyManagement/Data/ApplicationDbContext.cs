using Microsoft.EntityFrameworkCore;
using PropertyManagement.Models;

namespace PropertyManagement.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Property> Properties { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Lease> Leases { get; set; }
    public DbSet<RentPayment> RentPayments { get; set; }
    public DbSet<Expense>     Expenses     { get; set; }
    public DbSet<AuditLog>    AuditLogs    { get; set; }
    public DbSet<UserPropertyAssignment> UserPropertyAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPropertyAssignment>()
            .HasKey(x => new { x.UserId, x.PropertyId });
    }
}
