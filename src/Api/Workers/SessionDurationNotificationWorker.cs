using Misa.Application.Features.Items.Sessions.Commands;
using Wolverine;

namespace Misa.Api.Workers;

public sealed class SessionDurationNotificationWorker(
    IServiceProvider services,
    ILogger<SessionDurationNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SessionDurationNotificationWorker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

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
            logger.LogInformation("SessionDurationNotificationWorker stopped.");
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            await bus.InvokeAsync(new NotifySessionPlannedDurationReachedCommand(), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SessionDurationNotificationWorker tick failed.");
        }
    }
}
