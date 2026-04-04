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
    public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceRequest>()
            .HasOne(m => m.SubmittedBy)
            .WithMany()
            .HasForeignKey(m => m.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaintenanceRequest>()
            .HasOne(m => m.AssignedTo)
            .WithMany()
            .HasForeignKey(m => m.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
