using Microsoft.EntityFrameworkCore;
using PropertyManagement.Models;
using System.Security.Cryptography;
using System.Text;

namespace PropertyManagement.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.Users.AnyAsync()) return;

        // Seed admin user
        var admin = new ApplicationUser
        {
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@property.com",
            PasswordHash = HashPassword("Admin123!"),
            Role = UserRole.Administrator,
            IsActive = true
        };

        var manager = new ApplicationUser
        {
            FirstName = "Jane",
            LastName = "Manager",
            Email = "manager@property.com",
            PasswordHash = HashPassword("Manager123!"),
            Role = UserRole.PropertyManager,
            IsActive = true
        };

        var maintenance = new ApplicationUser
        {
            FirstName = "Bob",
            LastName = "Maintenance",
            Email = "maintenance@property.com",
            PasswordHash = HashPassword("Maint123!"),
            Role = UserRole.MaintenanceStaff,
            IsActive = true
        };

        var accounting = new ApplicationUser
        {
            FirstName = "Alice",
            LastName = "Accounting",
            Email = "accounting@property.com",
            PasswordHash = HashPassword("Acct123!"),
            Role = UserRole.AccountingTeam,
            IsActive = true
        };

        context.Users.AddRange(admin, manager, maintenance, accounting);

        // Seed properties
        var prop1 = new Property
        {
            Name = "Riverside Apartments",
            Address = "10 River Road",
            City = "Dublin",
            State = "Leinster",
            ZipCode = "D01 XY12",
            ContactPhone = "+353 1 234 5678",
            ContactEmail = "riverside@property.com"
        };

        var prop2 = new Property
        {
            Name = "City Centre Flats",
            Address = "5 O'Connell Street",
            City = "Dublin",
            State = "Leinster",
            ZipCode = "D01 AB34",
            ContactPhone = "+353 1 234 5679",
            ContactEmail = "citycentre@property.com"
        };

        context.Properties.AddRange(prop1, prop2);
        await context.SaveChangesAsync();

        // Seed units
        var unit1 = new Unit { PropertyId = prop1.Id, UnitNumber = "101", Type = "Apartment", Bedrooms = 2, Bathrooms = 1, FloorArea = 75, MonthlyRent = 1800, IsOccupied = true };
        var unit2 = new Unit { PropertyId = prop1.Id, UnitNumber = "102", Type = "Apartment", Bedrooms = 1, Bathrooms = 1, FloorArea = 55, MonthlyRent = 1400, IsOccupied = false };
        var unit3 = new Unit { PropertyId = prop2.Id, UnitNumber = "201", Type = "Studio", Bedrooms = 0, Bathrooms = 1, FloorArea = 35, MonthlyRent = 1100, IsOccupied = true };
        var unit4 = new Unit { PropertyId = prop2.Id, UnitNumber = "202", Type = "Apartment", Bedrooms = 3, Bathrooms = 2, FloorArea = 110, MonthlyRent = 2400, IsOccupied = false };

        context.Units.AddRange(unit1, unit2, unit3, unit4);

        // Seed tenants
        var tenant1 = new Tenant
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "john.smith@email.com",
            Phone = "+353 87 123 4567",
            EmployerName = "TechCorp Ltd",
            EmergencyContactName = "Mary Smith",
            EmergencyContactPhone = "+353 87 765 4321"
        };

        var tenant2 = new Tenant
        {
            FirstName = "Sarah",
            LastName = "Connor",
            Email = "sarah.connor@email.com",
            Phone = "+353 86 234 5678",
            EmployerName = "Finance Inc",
            EmergencyContactName = "Tom Connor",
            EmergencyContactPhone = "+353 86 876 5432"
        };

        context.Tenants.AddRange(tenant1, tenant2);
        await context.SaveChangesAsync();

        // Seed leases
        var lease1 = new Lease
        {
            TenantId = tenant1.Id,
            UnitId = unit1.Id,
            StartDate = DateTime.Today.AddMonths(-6),
            EndDate = DateTime.Today.AddMonths(6),
            MonthlyRent = 1800,
            SecurityDeposit = 3600,
            Status = LeaseStatus.Active
        };

        var lease2 = new Lease
        {
            TenantId = tenant2.Id,
            UnitId = unit3.Id,
            StartDate = DateTime.Today.AddMonths(-2),
            EndDate = DateTime.Today.AddMonths(10),
            MonthlyRent = 1100,
            SecurityDeposit = 2200,
            Status = LeaseStatus.Active
        };

        context.Leases.AddRange(lease1, lease2);
        await context.SaveChangesAsync();

        // Seed rent payments
        var payment1 = new RentPayment
        {
            LeaseId = lease1.Id,
            Amount = 1800,
            PaymentDate = DateTime.Today.AddMonths(-1),
            PaymentMethod = PaymentMethod.BankTransfer,
            OutstandingBalance = 0,
            RecordedByUserId = accounting.Id
        };

        context.RentPayments.Add(payment1);

        // Seed maintenance requests
        var mr1 = new MaintenanceRequest
        {
            PropertyId = prop1.Id,
            UnitId = unit1.Id,
            SubmittedByUserId = maintenance.Id,
            Title = "Leaking tap in bathroom",
            Description = "The hot water tap in the main bathroom is dripping constantly.",
            UrgencyLevel = UrgencyLevel.Medium,
            Status = MaintenanceStatus.Assigned,
            AssignedToUserId = maintenance.Id,
            Priority = "Normal"
        };

        var mr2 = new MaintenanceRequest
        {
            PropertyId = prop2.Id,
            UnitId = unit3.Id,
            SubmittedByUserId = maintenance.Id,
            Title = "Broken window latch",
            Description = "The latch on the living room window is broken and will not close securely.",
            UrgencyLevel = UrgencyLevel.High,
            Status = MaintenanceStatus.Open
        };

        context.MaintenanceRequests.AddRange(mr1, mr2);
        await context.SaveChangesAsync();
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
