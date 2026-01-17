using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Wolverine;

namespace Misa.Api.Services.Features.Items.Features.Sessions;

public class SessionPastMaxTimeWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SessionAutostopWorker> _logger;

    public SessionPastMaxTimeWorker(IServiceProvider services, ILogger<SessionAutostopWorker> logger)
    {
        _services = services;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        
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

            await bus.InvokeAsync(new PauseExpiredSessionsCommand(), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autostop tick failed.");
        }
    }
}