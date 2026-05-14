using PropertyManagement.Models;
using PropertyManagement.Services;
using PropertyManagement.Tests.Helpers;

namespace PropertyManagement.Tests.Services;

public class UserServiceTests
{
    static User MakeUser(int id, string email = "u@test.com", UserRole role = UserRole.PropertyManager) =>
        new() { Id = id, FirstName = "Jane", LastName = "Doe", Email = email, Role = role, IsActive = true };

    // ── CreateAsync / GetByEmailAsync ─────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_HashesPasswordAndPersistsUser()
    {
        using var db = DbHelper.CreateDb(nameof(CreateAsync_HashesPasswordAndPersistsUser));
        var user = MakeUser(0);

        await new UserService(db).CreateAsync(user, "Secret123");

        var saved = db.Users.Single();
        Assert.NotEqual("Secret123", saved.PasswordHash);
        Assert.False(string.IsNullOrEmpty(saved.PasswordHash));
        Assert.True(saved.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenActiveAndEmailMatches()
    {
        using var db = DbHelper.CreateDb(nameof(GetByEmailAsync_ReturnsUser_WhenActiveAndEmailMatches));
        await new UserService(db).CreateAsync(MakeUser(0, "find@test.com"), "pass");

        var result = await new UserService(db).GetByEmailAsync("find@test.com");

        Assert.NotNull(result);
        Assert.Equal("find@test.com", result!.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenUserInactive()
    {
        using var db = DbHelper.CreateDb(nameof(GetByEmailAsync_ReturnsNull_WhenUserInactive));
        var user = MakeUser(1, "inactive@test.com"); user.IsActive = false;
        db.Users.Add(user);
        db.SaveChanges();

        var result = await new UserService(db).GetByEmailAsync("inactive@test.com");
        Assert.Null(result);
    }

    // ── ValidateCredentialsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task ValidateCredentials_ReturnsUser_WhenPasswordCorrect()
    {
        using var db = DbHelper.CreateDb(nameof(ValidateCredentials_ReturnsUser_WhenPasswordCorrect));
        await new UserService(db).CreateAsync(MakeUser(0, "login@test.com"), "correct");

        var result = await new UserService(db).ValidateCredentialsAsync("login@test.com", "correct");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ValidateCredentials_ReturnsNull_WhenPasswordWrong()
    {
        using var db = DbHelper.CreateDb(nameof(ValidateCredentials_ReturnsNull_WhenPasswordWrong));
        await new UserService(db).CreateAsync(MakeUser(0, "login@test.com"), "correct");

        var result = await new UserService(db).ValidateCredentialsAsync("login@test.com", "wrong");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateCredentials_ReturnsNull_WhenEmailNotFound()
    {
        using var db = DbHelper.CreateDb(nameof(ValidateCredentials_ReturnsNull_WhenEmailNotFound));
        var result = await new UserService(db).ValidateCredentialsAsync("nobody@test.com", "pass");
        Assert.Null(result);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsWithoutChangingPassword()
    {
        using var db = DbHelper.CreateDb(nameof(UpdateAsync_UpdatesFieldsWithoutChangingPassword));
        await new UserService(db).CreateAsync(MakeUser(0), "original");
        var saved = db.Users.Single();
        var oldHash = saved.PasswordHash;

        await new UserService(db).UpdateAsync(new User { Id = saved.Id, FirstName = "Updated", LastName = "Name", Email = saved.Email, Role = UserRole.Administrator, IsActive = true });

        var updated = db.Users.Find(saved.Id)!;
        Assert.Equal("Updated", updated.FirstName);
        Assert.Equal(UserRole.Administrator, updated.Role);
        Assert.Equal(oldHash, updated.PasswordHash);
    }

    [Fact]
    public async Task UpdateAsync_ChangesPassword_WhenNewPasswordProvided()
    {
        using var db = DbHelper.CreateDb(nameof(UpdateAsync_ChangesPassword_WhenNewPasswordProvided));
        await new UserService(db).CreateAsync(MakeUser(0), "original");
        var saved = db.Users.Single();
        var oldHash = saved.PasswordHash;

        await new UserService(db).UpdateAsync(
            new User { Id = saved.Id, FirstName = "Jane", LastName = "Doe", Email = saved.Email, Role = UserRole.PropertyManager, IsActive = true },
            newPassword: "newpass");

        Assert.NotEqual(oldHash, db.Users.Find(saved.Id)!.PasswordHash);
    }

    // ── DeactivateAsync / ActivateAsync ───────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveFalse()
    {
        using var db = DbHelper.CreateDb(nameof(DeactivateAsync_SetsIsActiveFalse));
        db.Users.Add(MakeUser(1));
        db.SaveChanges();

        await new UserService(db).DeactivateAsync(1);

        Assert.False(db.Users.Find(1)!.IsActive);
    }

    [Fact]
    public async Task ActivateAsync_SetsIsActiveTrue()
    {
        using var db = DbHelper.CreateDb(nameof(ActivateAsync_SetsIsActiveTrue));
        var u = MakeUser(1); u.IsActive = false;
        db.Users.Add(u);
        db.SaveChanges();

        await new UserService(db).ActivateAsync(1);

        Assert.True(db.Users.Find(1)!.IsActive);
    }

    // ── GetAllAsync / GetByRoleAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsUsers_OrderedByLastName()
    {
        using var db = DbHelper.CreateDb(nameof(GetAllAsync_ReturnsUsers_OrderedByLastName));
        db.Users.AddRange(
            new User { FirstName = "A", LastName = "Zebra", Email = "z@t.com" },
            new User { FirstName = "B", LastName = "Apple", Email = "a@t.com" });
        db.SaveChanges();

        var result = await new UserService(db).GetAllAsync();

        Assert.Equal("Apple", result[0].LastName);
    }

    [Fact]
    public async Task GetByRoleAsync_ReturnsOnlyMatchingActiveUsers()
    {
        using var db = DbHelper.CreateDb(nameof(GetByRoleAsync_ReturnsOnlyMatchingActiveUsers));
        db.Users.AddRange(
            new User { FirstName = "A", LastName = "B", Email = "a@t.com", Role = UserRole.Administrator, IsActive = true },
            new User { FirstName = "C", LastName = "D", Email = "c@t.com", Role = UserRole.PropertyManager, IsActive = true });
        db.SaveChanges();

        var result = await new UserService(db).GetByRoleAsync(UserRole.Administrator);

        Assert.Single(result);
        Assert.Equal(UserRole.Administrator, result[0].Role);
    }

    // ── EmailExistsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EmailExistsAsync_ReturnsTrue_WhenEmailTaken()
    {
        using var db = DbHelper.CreateDb(nameof(EmailExistsAsync_ReturnsTrue_WhenEmailTaken));
        db.Users.Add(new User { Email = "taken@test.com", FirstName = "A", LastName = "B" });
        db.SaveChanges();

        Assert.True(await new UserService(db).EmailExistsAsync("taken@test.com"));
    }

    [Fact]
    public async Task EmailExistsAsync_ReturnsFalse_WhenExcludedIdMatches()
    {
        using var db = DbHelper.CreateDb(nameof(EmailExistsAsync_ReturnsFalse_WhenExcludedIdMatches));
        db.Users.Add(new User { Id = 1, Email = "mine@test.com", FirstName = "A", LastName = "B" });
        db.SaveChanges();

        Assert.False(await new UserService(db).EmailExistsAsync("mine@test.com", excludeId: 1));
    }

    // ── Property assignments ──────────────────────────────────────────────────

    [Fact]
    public async Task SetPropertyAssignmentsAsync_ReplacesExistingAssignments()
    {
        using var db = DbHelper.CreateDb(nameof(SetPropertyAssignmentsAsync_ReplacesExistingAssignments));
        db.UserPropertyAssignments.Add(new UserPropertyAssignment { UserId = 1, PropertyId = 10 });
        db.SaveChanges();

        await new UserService(db).SetPropertyAssignmentsAsync(1, [20, 30]);

        var ids = db.UserPropertyAssignments.Where(a => a.UserId == 1).Select(a => a.PropertyId).ToList();
        Assert.DoesNotContain(10, ids);
        Assert.Contains(20, ids);
        Assert.Contains(30, ids);
    }

    [Fact]
    public async Task GetAssignedPropertyIdsAsync_ReturnsIds()
    {
        using var db = DbHelper.CreateDb(nameof(GetAssignedPropertyIdsAsync_ReturnsIds));
        db.UserPropertyAssignments.AddRange(
            new UserPropertyAssignment { UserId = 1, PropertyId = 5 },
            new UserPropertyAssignment { UserId = 1, PropertyId = 7 });
        db.SaveChanges();

        var ids = await new UserService(db).GetAssignedPropertyIdsAsync(1);

        Assert.Equal(2, ids.Count);
        Assert.Contains(5, ids);
    }

    [Fact]
    public async Task GetAssignmentCountsAsync_ReturnsCounts()
    {
        using var db = DbHelper.CreateDb(nameof(GetAssignmentCountsAsync_ReturnsCounts));
        db.UserPropertyAssignments.AddRange(
            new UserPropertyAssignment { UserId = 1, PropertyId = 1 },
            new UserPropertyAssignment { UserId = 1, PropertyId = 2 },
            new UserPropertyAssignment { UserId = 2, PropertyId = 1 });
        db.SaveChanges();

        var counts = await new UserService(db).GetAssignmentCountsAsync();

        Assert.Equal(2, counts[1]);
        Assert.Equal(1, counts[2]);
    }

    // ── UpdateLastLoginAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLastLoginAsync_SetsLastLogin()
    {
        using var db = DbHelper.CreateDb(nameof(UpdateLastLoginAsync_SetsLastLogin));
        db.Users.Add(MakeUser(1));
        db.SaveChanges();

        await new UserService(db).UpdateLastLoginAsync(1);

        Assert.NotNull(db.Users.Find(1)!.LastLogin);
    }
}
