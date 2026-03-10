using Microsoft.EntityFrameworkCore;
using PropertyManagement.Models;

namespace PropertyManagement.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> Users { get; set; }
    public DbSet<Property> Properties { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Lease> Leases { get; set; }
    public DbSet<RentPayment> RentPayments { get; set; }
    public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Property>().ToTable("Properties");
        modelBuilder.Entity<Unit>().ToTable("Units");
        modelBuilder.Entity<Tenant>().ToTable("Tenants");
        modelBuilder.Entity<Lease>().ToTable("Leases");
        modelBuilder.Entity<RentPayment>().ToTable("RentPayments");
        modelBuilder.Entity<MaintenanceRequest>().ToTable("MaintenanceRequests");
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<AuditLog>().ToTable("AuditLogs");

        modelBuilder.Entity<Unit>()
            .HasOne(u => u.Property)
            .WithMany(p => p.Units)
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Lease>()
            .HasOne(l => l.Tenant)
            .WithMany(t => t.Leases)
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Lease>()
            .HasOne(l => l.Unit)
            .WithMany(u => u.Leases)
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RentPayment>()
            .HasOne(r => r.Lease)
            .WithMany(l => l.RentPayments)
            .HasForeignKey(r => r.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaintenanceRequest>()
            .HasOne(m => m.Property)
            .WithMany()
            .HasForeignKey(m => m.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaintenanceRequest>()
            .HasOne(m => m.Unit)
            .WithMany(u => u.MaintenanceRequests)
            .HasForeignKey(m => m.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RentPayment>().Property(r => r.Amount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<RentPayment>().Property(r => r.OutstandingBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Lease>().Property(l => l.MonthlyRent).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Lease>().Property(l => l.SecurityDeposit).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Unit>().Property(u => u.MonthlyRent).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<MaintenanceRequest>().Property(m => m.EstimatedCost).HasColumnType("decimal(18,2)");
    }
}
