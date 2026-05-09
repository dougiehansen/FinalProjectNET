using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Models;

namespace PropertyManagement.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        // Add ManagerConfirmed column if upgrading an existing database
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        bool hasCol;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Leases') WHERE name='ManagerConfirmed'";
            hasCol = (long)(await cmd.ExecuteScalarAsync())! > 0;
        }
        if (!hasCol)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE Leases ADD COLUMN ManagerConfirmed INTEGER NOT NULL DEFAULT 0";
            await cmd.ExecuteNonQueryAsync();
        }

        async Task AddColIfMissing(string column, string definition)
        {
            using var c = conn.CreateCommand();
            c.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('Leases') WHERE name='{column}'";
            if ((long)(await c.ExecuteScalarAsync())! == 0)
            {
                using var a = conn.CreateCommand();
                a.CommandText = $"ALTER TABLE Leases ADD COLUMN {column} {definition}";
                await a.ExecuteNonQueryAsync();
            }
        }

        await AddColIfMissing("SigningPageOpenedAt", "TEXT NULL");
        await AddColIfMissing("TenantIpAddress",     "TEXT NULL");
        await AddColIfMissing("TenantUserAgent",     "TEXT NULL");

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS AuditLogs (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Action      TEXT NOT NULL,
                TargetName  TEXT NOT NULL,
                TargetEmail TEXT NOT NULL,
                Details     TEXT NULL,
                PerformedBy TEXT NOT NULL,
                CreatedAt   TEXT NOT NULL
            )";
            await cmd.ExecuteNonQueryAsync();
        }

        await conn.CloseAsync();

        if (db.Users.Any()) return;

        // Users
        var hasher = new PasswordHasher<User>();
        var users = new[]
        {
            new User { FirstName = "Admin",       LastName = "User",    Email = "admin@property.com",       Role = UserRole.Administrator },
            new User { FirstName = "Code",        LastName = "Testing", Email = "codetestingtu@gmail.com",   Role = UserRole.Administrator },
            new User { FirstName = "Property",    LastName = "Manager", Email = "manager@property.com",     Role = UserRole.PropertyManager },
            new User { FirstName = "Accounting",  LastName = "Staff",   Email = "accounting@property.com",  Role = UserRole.AccountingTeam },
        };
        string[] passwords = { "Admin123!", "Admin123!", "Manager123!", "Acct123!" };

        for (int i = 0; i < users.Length; i++)
            users[i].PasswordHash = hasher.HashPassword(users[i], passwords[i]);

        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        // Properties
        var properties = new[]
        {
            new Property
            {
                Name = "Liffey Court Apartments",
                Address = "14 Ormond Quay Upper",
                City = "Dublin",
                State = "Dublin 7",
                ZipCode = "D07 F8X2",
                ContactPhone = "01 872 4400",
                ContactEmail = "liffey@dublinproperties.ie",
                Latitude = 53.3461,
                Longitude = -6.2675
            },
            new Property
            {
                Name = "Grand Canal Residences",
                Address = "8 Mespil Road",
                City = "Dublin",
                State = "Dublin 4",
                ZipCode = "D04 K2P1",
                ContactPhone = "01 660 3100",
                ContactEmail = "grandcanal@dublinproperties.ie",
                Latitude = 53.3318,
                Longitude = -6.2433
            },
            new Property
            {
                Name = "Stoneybatter House",
                Address = "22 Manor Street",
                City = "Dublin",
                State = "Dublin 7",
                ZipCode = "D07 A3N4",
                ContactPhone = "01 838 5500",
                ContactEmail = "stoneybatter@dublinproperties.ie",
                Latitude = 53.3489,
                Longitude = -6.2794
            }
        };

        db.Properties.AddRange(properties);
        await db.SaveChangesAsync();

        // Units
        var units = new[]
        {
            // Liffey Court
            new Unit { PropertyId = properties[0].Id, UnitNumber = "101", Type = "1 Bed Apartment", FloorArea = 52m, Bedrooms = 1, Bathrooms = 1, MonthlyRent = 1850m, Amenities = "Parking, Balcony", IsOccupied = true },
            new Unit { PropertyId = properties[0].Id, UnitNumber = "102", Type = "2 Bed Apartment", FloorArea = 75m, Bedrooms = 2, Bathrooms = 1, MonthlyRent = 2400m, Amenities = "Parking, Balcony, Storage", IsOccupied = true },
            new Unit { PropertyId = properties[0].Id, UnitNumber = "103", Type = "Studio",          FloorArea = 38m, Bedrooms = 0, Bathrooms = 1, MonthlyRent = 1500m, Amenities = "Gym Access", IsOccupied = false },
            new Unit { PropertyId = properties[0].Id, UnitNumber = "201", Type = "2 Bed Apartment", FloorArea = 80m, Bedrooms = 2, Bathrooms = 2, MonthlyRent = 2600m, Amenities = "Parking, Balcony, Concierge", IsOccupied = true },

            // Grand Canal
            new Unit { PropertyId = properties[1].Id, UnitNumber = "101", Type = "1 Bed Apartment", FloorArea = 55m, Bedrooms = 1, Bathrooms = 1, MonthlyRent = 2100m, Amenities = "Canal View, Gym", IsOccupied = true },
            new Unit { PropertyId = properties[1].Id, UnitNumber = "102", Type = "2 Bed Apartment", FloorArea = 82m, Bedrooms = 2, Bathrooms = 2, MonthlyRent = 2950m, Amenities = "Canal View, Parking, Gym", IsOccupied = true },
            new Unit { PropertyId = properties[1].Id, UnitNumber = "201", Type = "1 Bed Apartment", FloorArea = 55m, Bedrooms = 1, Bathrooms = 1, MonthlyRent = 2100m, Amenities = "Canal View, Gym", IsOccupied = false },

            // Stoneybatter
            new Unit { PropertyId = properties[2].Id, UnitNumber = "1",   Type = "1 Bed Apartment", FloorArea = 48m, Bedrooms = 1, Bathrooms = 1, MonthlyRent = 1700m, Amenities = "Garden Access", IsOccupied = true },
            new Unit { PropertyId = properties[2].Id, UnitNumber = "2",   Type = "2 Bed Apartment", FloorArea = 70m, Bedrooms = 2, Bathrooms = 1, MonthlyRent = 2200m, Amenities = "Garden Access, Storage", IsOccupied = true },
            new Unit { PropertyId = properties[2].Id, UnitNumber = "3",   Type = "Studio",          FloorArea = 35m, Bedrooms = 0, Bathrooms = 1, MonthlyRent = 1400m, Amenities = "Garden Access", IsOccupied = false },
        };

        db.Units.AddRange(units);
        await db.SaveChangesAsync();

        // Tenants
        var tenants = new[]
        {
            new Tenant { FirstName = "Aoife",   LastName = "Murphy",    Email = "aoife.murphy@gmail.com",    Phone = "087 123 4567", DateOfBirth = new DateTime(1992, 3, 14), EmployerName = "AIB Bank",         EmployerPhone = "01 660 0311", EmergencyContactName = "Ciaran Murphy",   EmergencyContactPhone = "087 234 5678" },
            new Tenant { FirstName = "Ciarán",  LastName = "O'Brien",   Email = "ciaran.obrien@gmail.com",   Phone = "086 234 5678", DateOfBirth = new DateTime(1988, 7, 22), EmployerName = "Google Ireland",   EmployerPhone = "01 543 1000", EmergencyContactName = "Sinéad O'Brien",  EmergencyContactPhone = "085 345 6789" },
            new Tenant { FirstName = "Sinéad",  LastName = "Kelly",     Email = "sinead.kelly@gmail.com",    Phone = "085 345 6789", DateOfBirth = new DateTime(1995, 11, 5), EmployerName = "HSE",              EmployerPhone = "01 635 2000", EmergencyContactName = "Pádraig Kelly",   EmergencyContactPhone = "086 456 7890" },
            new Tenant { FirstName = "Pádraig", LastName = "Walsh",     Email = "padraig.walsh@gmail.com",   Phone = "083 456 7890", DateOfBirth = new DateTime(1990, 1, 30), EmployerName = "Accenture",        EmployerPhone = "01 646 2000", EmergencyContactName = "Máire Walsh",     EmergencyContactPhone = "087 567 8901" },
            new Tenant { FirstName = "Niamh",   LastName = "Byrne",     Email = "niamh.byrne@gmail.com",     Phone = "087 567 8901", DateOfBirth = new DateTime(1993, 6, 18), EmployerName = "Dublin City Council", EmployerPhone = "01 222 2222", EmergencyContactName = "Seán Byrne",   EmergencyContactPhone = "086 678 9012" },
            new Tenant { FirstName = "Seán",    LastName = "Doyle",     Email = "sean.doyle@gmail.com",      Phone = "086 678 9012", DateOfBirth = new DateTime(1985, 9, 25), EmployerName = "Deloitte Ireland", EmployerPhone = "01 417 2200", EmergencyContactName = "Aoife Doyle",    EmergencyContactPhone = "085 789 0123" },
            new Tenant { FirstName = "Caoimhe", LastName = "Fitzgerald", Email = "caoimhe.fitz@gmail.com",  Phone = "085 789 0123", DateOfBirth = new DateTime(1997, 4, 12), EmployerName = "Penneys",          EmployerPhone = "01 872 7788", EmergencyContactName = "Tomás Fitzgerald", EmergencyContactPhone = "083 890 1234" },
        };

        db.Tenants.AddRange(tenants);
        await db.SaveChangesAsync();

        // Leases (for occupied units)
        var leases = new[]
        {
            new Lease { UnitId = units[0].Id, TenantId = tenants[0].Id, StartDate = new DateTime(2025, 1, 1),  EndDate = new DateTime(2025, 12, 31), MonthlyRent = 1850m, SecurityDeposit = 3700m, Status = LeaseStatus.Active  },
            new Lease { UnitId = units[1].Id, TenantId = tenants[1].Id, StartDate = new DateTime(2025, 3, 1),  EndDate = new DateTime(2026, 2, 28), MonthlyRent = 2400m, SecurityDeposit = 4800m, Status = LeaseStatus.Active  },
            new Lease { UnitId = units[3].Id, TenantId = tenants[2].Id, StartDate = new DateTime(2025, 6, 1),  EndDate = new DateTime(2026, 5, 31), MonthlyRent = 2600m, SecurityDeposit = 5200m, Status = LeaseStatus.Active  },
            new Lease { UnitId = units[4].Id, TenantId = tenants[3].Id, StartDate = new DateTime(2025, 2, 1),  EndDate = new DateTime(2026, 1, 31), MonthlyRent = 2100m, SecurityDeposit = 4200m, Status = LeaseStatus.Active  },
            new Lease { UnitId = units[5].Id, TenantId = tenants[4].Id, StartDate = new DateTime(2025, 9, 1),  EndDate = new DateTime(2026, 8, 31), MonthlyRent = 2950m, SecurityDeposit = 5900m, Status = LeaseStatus.Active  },
            new Lease { UnitId = units[7].Id, TenantId = tenants[5].Id, StartDate = new DateTime(2025, 4, 1),  EndDate = new DateTime(2026, 3, 31), MonthlyRent = 1700m, SecurityDeposit = 3400m, Status = LeaseStatus.Active  },
            new Lease { UnitId = units[8].Id, TenantId = tenants[6].Id, StartDate = new DateTime(2025, 7, 1),  EndDate = new DateTime(2026, 6, 30), MonthlyRent = 2200m, SecurityDeposit = 4400m, Status = LeaseStatus.Active  },
        };

        db.Leases.AddRange(leases);
        await db.SaveChangesAsync();

        // Rent Payments
        var payments = new[]
        {
            new RentPayment { LeaseId = leases[0].Id, Amount = 1850m, PaymentDate = new DateTime(2025, 11, 1), PaymentMethod = PaymentMethod.BankTransfer, OutstandingBalance = 0m, RecordedByUserId = users[4].Id },
            new RentPayment { LeaseId = leases[0].Id, Amount = 1850m, PaymentDate = new DateTime(2025, 12, 1), PaymentMethod = PaymentMethod.BankTransfer, OutstandingBalance = 0m, RecordedByUserId = users[4].Id },
            new RentPayment { LeaseId = leases[1].Id, Amount = 2400m, PaymentDate = new DateTime(2025, 11, 1), PaymentMethod = PaymentMethod.BankTransfer, OutstandingBalance = 0m, RecordedByUserId = users[4].Id },
            new RentPayment { LeaseId = leases[1].Id, Amount = 2400m, PaymentDate = new DateTime(2025, 12, 1), PaymentMethod = PaymentMethod.BankTransfer, OutstandingBalance = 0m, RecordedByUserId = users[4].Id },
            new RentPayment { LeaseId = leases[2].Id, Amount = 2600m, PaymentDate = new DateTime(2025, 11, 1), PaymentMethod = PaymentMethod.BankTransfer, OutstandingBalance = 0m, RecordedByUserId = users[4].Id },
            new RentPayment { LeaseId = leases[3].Id, Amount = 2100m, PaymentDate = new DateTime(2025, 11, 1), PaymentMethod = PaymentMethod.BankTransfer, OutstandingBalance = 0m, RecordedByUserId = users[4].Id },
            new RentPayment { LeaseId = leases[3].Id, Amount = 2100m, PaymentDate = new DateTime(2025, 12, 1), PaymentMethod = PaymentMethod.BankTransfer, OutstandingBalance = 0m, RecordedByUserId = users[4].Id },
            new RentPayment { LeaseId = leases[4].Id, Amount = 2950m, PaymentDate = new DateTime(2025, 11, 1), PaymentMethod = PaymentMethod.BankTransfer, OutstandingBalance = 0m, RecordedByUserId = users[4].Id },
            new RentPayment { LeaseId = leases[5].Id, Amount = 1700m, PaymentDate = new DateTime(2025, 11, 1), PaymentMethod = PaymentMethod.BankTransfer, OutstandingBalance = 0m, RecordedByUserId = users[4].Id },
            new RentPayment { LeaseId = leases[5].Id, Amount = 1700m, PaymentDate = new DateTime(2025, 12, 1), PaymentMethod = PaymentMethod.BankTransfer, OutstandingBalance = 0m, RecordedByUserId = users[4].Id },
            new RentPayment { LeaseId = leases[6].Id, Amount = 2200m, PaymentDate = new DateTime(2025, 11, 1), PaymentMethod = PaymentMethod.BankTransfer, OutstandingBalance = 0m, RecordedByUserId = users[4].Id },
        };

        db.RentPayments.AddRange(payments);
        await db.SaveChangesAsync();

    }
}
