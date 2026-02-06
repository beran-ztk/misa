using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Wolverine;

namespace Misa.Api.Services.Features.Items.Features.Scheduler;

public class SchedulePlanningWorker(IServiceProvider provider, ILogger<SchedulePlanningWorker> logger) : BackgroundService
{
    private int _isRunning = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Schedule planning worker activated.");
        
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnce(stoppingToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
        finally
        {
            logger.LogInformation("Schedule planning worker deactivated.");
        }
    }

    private async Task RunOnce(CancellationToken stoppingToken)
    {
        if (Interlocked.Exchange(ref _isRunning, 1) == 1)
            return;
        
        try
        {
            logger.LogInformation("Schedule planning worker running at.");

            var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            await bus.InvokeAsync(new SchedulePlanningCommand(), stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schedule planning worker failed at.");
        }
        finally
        {
            logger.LogInformation("Schedule planning worker finished at.");
            
            Interlocked.Exchange(ref _isRunning, 0);
        }
    }
}