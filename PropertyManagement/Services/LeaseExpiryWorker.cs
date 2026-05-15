namespace PropertyManagement.Services;

public class LeaseExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaseExpiryWorker> _logger;

    public LeaseExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<LeaseExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope  = _scopeFactory.CreateScope();
                var leaseService = scope.ServiceProvider.GetRequiredService<ILeaseService>();
                int count        = await leaseService.AutoExpireAsync();
                if (count > 0)
                    _logger.LogInformation("LeaseExpiryWorker: auto-expired {Count} lease(s).", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LeaseExpiryWorker error.");
            }

            // wait 24 hours before running again, exit cleanly if app is shutting down
            try { await Task.Delay(TimeSpan.FromHours(24), stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }
}
