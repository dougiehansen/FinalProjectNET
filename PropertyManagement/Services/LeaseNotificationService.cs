namespace PropertyManagement.Services;

public class LeaseNotificationService
{
    public event Func<int, Task>? OnLeaseSigned;

    public async Task NotifyLeaseSignedAsync(int leaseId)
    {
        if (OnLeaseSigned is not { } handler) return;
        await Task.WhenAll(handler.GetInvocationList()
            .Cast<Func<int, Task>>()
            .Select(h => h(leaseId)));
    }
}
