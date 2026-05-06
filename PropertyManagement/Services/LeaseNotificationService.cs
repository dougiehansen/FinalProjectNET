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
        if (OnSigningActivity is not { } handler) return;
        await Task.WhenAll(handler.GetInvocationList()
            .Cast<Func<SigningActivity, Task>>()
            .Select(h => h(activity)));
    }
}
