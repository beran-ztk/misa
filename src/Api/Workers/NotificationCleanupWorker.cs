using Misa.Application.Features.Notifications;
using Wolverine;

namespace Misa.Api.Workers;

public sealed class NotificationCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationCleanupWorker> _logger;

    public NotificationCleanupWorker(IServiceProvider services, ILogger<NotificationCleanupWorker> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationCleanupWorker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnce(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _logger.LogInformation("NotificationCleanupWorker stopped.");
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var deleted = await bus.InvokeAsync<int>(new CleanupOldNotificationsCommand(), ct);

            if (deleted > 0)
                _logger.LogInformation("Notification cleanup: marked {Count} notification(s) as deleted.", deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification cleanup tick failed.");
        }
    }
}
