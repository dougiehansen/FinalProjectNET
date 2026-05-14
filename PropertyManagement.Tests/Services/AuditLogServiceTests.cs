using PropertyManagement.Services;
using PropertyManagement.Tests.Helpers;

namespace PropertyManagement.Tests.Services;

public class AuditLogServiceTests
{
    [Fact]
    public async Task LogAsync_PersistsAuditEntry()
    {
        using var db = DbHelper.CreateDb(nameof(LogAsync_PersistsAuditEntry));
        await new AuditLogService(db).LogAsync("Create", "John Smith", "j@s.com", "admin@pm.com", "Created via UI");

        var log = db.AuditLogs.Single();
        Assert.Equal("Create", log.Action);
        Assert.Equal("John Smith", log.TargetName);
        Assert.Equal("j@s.com", log.TargetEmail);
        Assert.Equal("admin@pm.com", log.PerformedBy);
        Assert.Equal("Created via UI", log.Details);
        Assert.True(log.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task LogAsync_AcceptsNullDetails()
    {
        using var db = DbHelper.CreateDb(nameof(LogAsync_AcceptsNullDetails));
        await new AuditLogService(db).LogAsync("Delete", "Jane", "j@x.com", "admin");
        Assert.Null(db.AuditLogs.Single().Details);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsLogsOrderedByDateDesc()
    {
        using var db = DbHelper.CreateDb(nameof(GetRecentAsync_ReturnsLogsOrderedByDateDesc));
        var svc = new AuditLogService(db);
        await svc.LogAsync("A", "T1", "e1@x.com", "admin");
        await svc.LogAsync("B", "T2", "e2@x.com", "admin");

        var logs = await svc.GetRecentAsync();

        Assert.Equal(2, logs.Count);
        Assert.True(logs[0].CreatedAt >= logs[1].CreatedAt);
    }

    [Fact]
    public async Task GetRecentAsync_RespectsCountLimit()
    {
        using var db = DbHelper.CreateDb(nameof(GetRecentAsync_RespectsCountLimit));
        var svc = new AuditLogService(db);
        for (int i = 0; i < 10; i++)
            await svc.LogAsync("X", $"T{i}", $"e{i}@x.com", "admin");

        var logs = await svc.GetRecentAsync(3);
        Assert.Equal(3, logs.Count);
    }

    [Fact]
    public async Task GetTodayCountAsync_ReturnsCountOfTodaysEntries()
    {
        using var db = DbHelper.CreateDb(nameof(GetTodayCountAsync_ReturnsCountOfTodaysEntries));
        var svc = new AuditLogService(db);
        await svc.LogAsync("A", "T1", "e@x.com", "admin");
        await svc.LogAsync("B", "T2", "f@x.com", "admin");

        var count = await svc.GetTodayCountAsync();
        Assert.Equal(2, count);
    }
}
