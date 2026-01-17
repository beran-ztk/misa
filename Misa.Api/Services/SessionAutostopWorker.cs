using Misa.Application.Items.Features.Sessions.Commands;
using Wolverine;

namespace Misa.Api.Services;

public sealed class SessionAutostopWorker : BackgroundService
{
    private readonly IMessageBus _bus;
    private readonly ILogger<SessionAutostopWorker> _logger;

    public SessionAutostopWorker(IMessageBus bus, ILogger<SessionAutostopWorker> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionAutostopWorker started.");

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
            // normal shutdown
        }
        finally
        {
            _logger.LogInformation("SessionAutostopWorker stopped.");
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        try
        {
            var stoppedCount = await _bus.InvokeAsync<int>(new StopExpiredSessionsCommand(), ct);

            if (stoppedCount > 0)
                _logger.LogInformation("Autostop: stopped {Count} session(s).", stoppedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autostop tick failed.");
        }
    }
}