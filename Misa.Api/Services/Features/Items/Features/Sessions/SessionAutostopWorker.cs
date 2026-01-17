using Misa.Application.Items.Features.Sessions.Commands;
using Wolverine;

namespace Misa.Api.Services.Features.Items.Features.Sessions;

public sealed class SessionAutostopWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SessionAutostopWorker> _logger;

    public SessionAutostopWorker(IServiceProvider services, ILogger<SessionAutostopWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionAutostopWorker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

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
            _logger.LogInformation("SessionAutostopWorker stopped.");
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        try
        {
            var scope = _services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            
            var stoppedCount = await bus.InvokeAsync<int>(new PauseDueSessionsCommand(), ct);

            if (stoppedCount > 0)
                _logger.LogInformation("Autostop: stopped {Count} session(s).", stoppedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autostop tick failed.");
        }
    }
}