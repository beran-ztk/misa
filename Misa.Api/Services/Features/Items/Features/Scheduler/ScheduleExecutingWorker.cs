using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Wolverine;

namespace Misa.Api.Services.Features.Items.Features.Scheduler;

public class ScheduleExecutingWorker(IServiceProvider provider, ILogger<ScheduleExecutingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Schedule executing worker activated.");
        
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

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
        try
        {
            logger.LogInformation("Schedule executing worker running at {DateTimeOffset:HH:mm:ss}.",
                DateTimeOffset.UtcNow);

            var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            await bus.InvokeAsync(new ScheduleExecutingCommand(), stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schedule executing worker failed at {DateTimeOffset:HH:mm:ss}.", 
                DateTimeOffset.UtcNow);
        }
        finally
        {
            logger.LogInformation("Schedule executing worker finished at {DateTimeOffset:HH:mm:ss}.",
                DateTimeOffset.UtcNow);
        }
    }
}