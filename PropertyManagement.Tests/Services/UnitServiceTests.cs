using PropertyManagement.Models;
using PropertyManagement.Services;
using PropertyManagement.Tests.Helpers;

namespace PropertyManagement.Tests.Services;

public class UnitServiceTests
{
    static void SeedProperty(PropertyManagement.Data.ApplicationDbContext db, int id = 1) =>
        db.Properties.Add(new Property { Id = id, Name = $"P{id}", Address = "1 St", City = "C", IsActive = true });

    static Unit MakeUnit(int id, int propId, string number = "A1") =>
        new() { Id = id, PropertyId = propId, UnitNumber = number, MonthlyRent = 1000, IsActive = true };

    [Fact]
    public async Task GetByPropertyAsync_ReturnsActiveUnits_OrderedByNumber()
    {
        using var db = DbHelper.CreateDb(nameof(GetByPropertyAsync_ReturnsActiveUnits_OrderedByNumber));
        SeedProperty(db);
        db.Units.AddRange(MakeUnit(1, 1, "B2"), MakeUnit(2, 1, "A1"));
        db.SaveChanges();

        var result = await new UnitService(db).GetByPropertyAsync(1);

        Assert.Equal(2, result.Count);
        Assert.Equal("A1", result[0].UnitNumber);
    }

    [Fact]
    public async Task GetByPropertyAsync_ExcludesInactiveUnits()
    {
        using var db = DbHelper.CreateDb(nameof(GetByPropertyAsync_ExcludesInactiveUnits));
        SeedProperty(db);
        var inactive = MakeUnit(1, 1); inactive.IsActive = false;
        db.Units.Add(inactive);
        db.SaveChanges();

        var result = await new UnitService(db).GetByPropertyAsync(1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateAsync_PersistsUnitAndSetsCreatedAt()
    {
        using var db = DbHelper.CreateDb(nameof(CreateAsync_PersistsUnitAndSetsCreatedAt));
        SeedProperty(db);
        db.SaveChanges();

        var unit = new Unit { PropertyId = 1, UnitNumber = "C3", MonthlyRent = 1200, Bedrooms = 2, IsActive = true };
        await new UnitService(db).CreateAsync(unit);

        var saved = db.Units.Single();
        Assert.Equal("C3", saved.UnitNumber);
        Assert.True(saved.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllFields()
    {
        using var db = DbHelper.CreateDb(nameof(UpdateAsync_UpdatesAllFields));
        SeedProperty(db);
        db.Units.Add(MakeUnit(1, 1));
        db.SaveChanges();

        await new UnitService(db).UpdateAsync(new Unit
        {
            Id = 1, PropertyId = 1, UnitNumber = "Z9", Type = "Studio",
            FloorArea = 45, Bedrooms = 1, Bathrooms = 1, MonthlyRent = 1500, Amenities = "WiFi"
        });

        var saved = db.Units.Find(1)!;
        Assert.Equal("Z9", saved.UnitNumber);
        Assert.Equal(1500, saved.MonthlyRent);
        Assert.Equal("WiFi", saved.Amenities);
    }

    [Fact]
    public async Task UpdateAsync_DoesNothing_WhenUnitNotFound()
    {
        using var db = DbHelper.CreateDb(nameof(UpdateAsync_DoesNothing_WhenUnitNotFound));
        SeedProperty(db);
        db.SaveChanges();

        await new UnitService(db).UpdateAsync(new Unit { Id = 99, PropertyId = 1, UnitNumber = "X" });
        Assert.Empty(db.Units);
    }

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveFalseAndClearsOccupied()
    {
        using var db = DbHelper.CreateDb(nameof(DeactivateAsync_SetsIsActiveFalseAndClearsOccupied));
        SeedProperty(db);
        var unit = MakeUnit(1, 1); unit.IsOccupied = true;
        db.Units.Add(unit);
        db.SaveChanges();

        await new UnitService(db).DeactivateAsync(1);

        var saved = db.Units.Find(1)!;
        Assert.False(saved.IsActive);
        Assert.False(saved.IsOccupied);
    }

    [Fact]
    public async Task DeactivateAsync_DoesNothing_WhenUnitNotFound()
    {
        using var db = DbHelper.CreateDb(nameof(DeactivateAsync_DoesNothing_WhenUnitNotFound));
        var ex = await Record.ExceptionAsync(() => new UnitService(db).DeactivateAsync(99));
        Assert.Null(ex);
    }
}
