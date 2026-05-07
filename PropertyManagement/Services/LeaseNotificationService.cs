namespace PropertyManagement.Services;

public record SigningActivity(
    int LeaseId,
    string TenantName,
    string Location,
    string? IpAddress,
    string? UserAgent,
    DateTime Timestamp,
    bool IsSigned
);

public class LeaseNotificationService
{
    public event Func<SigningActivity, Task>? OnSigningActivity;

    public async Task NotifyAsync(SigningActivity activity)
    {
        var count = OnSigningActivity?.GetInvocationList().Length ?? 0;
        Console.WriteLine($"[NOTIFY] LeaseId={activity.LeaseId} IsSigned={activity.IsSigned} Subscribers={count}");

        if (OnSigningActivity is not { } handler) return;
        await Task.WhenAll(handler.GetInvocationList()
            .Cast<Func<SigningActivity, Task>>()
            .Select(h => h(activity)));

        Console.WriteLine($"[NOTIFY] Done LeaseId={activity.LeaseId}");
    }
}
