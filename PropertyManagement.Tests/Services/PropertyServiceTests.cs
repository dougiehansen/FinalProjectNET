using PropertyManagement.Models;
using PropertyManagement.Services;
using PropertyManagement.Tests.Helpers;

namespace PropertyManagement.Tests.Services;

public class PropertyServiceTests
{
    static Property MakeProp(int id, string name = "Prop") =>
        new() { Id = id, Name = name, Address = "1 St", City = "Dublin", IsActive = true };

    [Fact]
    public async Task GetAllAsync_ReturnsProperties_OrderedByName()
    {
        using var db = DbHelper.CreateDb(nameof(GetAllAsync_ReturnsProperties_OrderedByName));
        db.Properties.AddRange(MakeProp(1, "Zebra House"), MakeProp(2, "Apple Court"));
        db.SaveChanges();

        var result = await new PropertyService(db).GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Apple Court", result[0].Name);
    }

    [Fact]
    public async Task CreateAsync_PersistsPropertyAndSetsCreatedAt()
    {
        using var db = DbHelper.CreateDb(nameof(CreateAsync_PersistsPropertyAndSetsCreatedAt));
        var prop = MakeProp(0, "New Prop");

        await new PropertyService(db).CreateAsync(prop);

        var saved = db.Properties.Single();
        Assert.Equal("New Prop", saved.Name);
        Assert.True(saved.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllFields()
    {
        using var db = DbHelper.CreateDb(nameof(UpdateAsync_UpdatesAllFields));
        db.Properties.Add(MakeProp(1, "Old Name"));
        db.SaveChanges();

        await new PropertyService(db).UpdateAsync(new Property
        {
            Id = 1, Name = "New Name", Address = "2 Ave", City = "Cork",
            State = "Munster", ZipCode = "T12", ContactPhone = "021",
            ContactEmail = "m@p.com", Latitude = 51.9, Longitude = -8.5
        });

        var saved = db.Properties.Find(1)!;
        Assert.Equal("New Name", saved.Name);
        Assert.Equal("Cork", saved.City);
        Assert.Equal(51.9, saved.Latitude);
    }

    [Fact]
    public async Task UpdateAsync_DoesNothing_WhenPropertyNotFound()
    {
        using var db = DbHelper.CreateDb(nameof(UpdateAsync_DoesNothing_WhenPropertyNotFound));
        await new PropertyService(db).UpdateAsync(new Property { Id = 99, Name = "Ghost", Address = "X", City = "X" });
        Assert.Empty(db.Properties);
    }

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveFalse()
    {
        using var db = DbHelper.CreateDb(nameof(DeactivateAsync_SetsIsActiveFalse));
        db.Properties.Add(MakeProp(1));
        db.SaveChanges();

        await new PropertyService(db).DeactivateAsync(1);

        Assert.False(db.Properties.Find(1)!.IsActive);
    }

    [Fact]
    public async Task ReactivateAsync_SetsIsActiveTrue()
    {
        using var db = DbHelper.CreateDb(nameof(ReactivateAsync_SetsIsActiveTrue));
        var p = MakeProp(1); p.IsActive = false;
        db.Properties.Add(p);
        db.SaveChanges();

        await new PropertyService(db).ReactivateAsync(1);

        Assert.True(db.Properties.Find(1)!.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_IncludesUnits()
    {
        using var db = DbHelper.CreateDb(nameof(GetAllAsync_IncludesUnits));
        var prop = MakeProp(1);
        db.Properties.Add(prop);
        db.Units.Add(new Unit { Id = 1, PropertyId = 1, UnitNumber = "A", MonthlyRent = 900, IsActive = true });
        db.SaveChanges();

        var result = await new PropertyService(db).GetAllAsync();

        Assert.Single(result[0].Units);
    }
}
