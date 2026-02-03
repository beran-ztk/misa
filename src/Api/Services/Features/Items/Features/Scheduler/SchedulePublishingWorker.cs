using Microsoft.AspNetCore.SignalR;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Wolverine;
using Misa.Api.Common.Hubs;

namespace Misa.Api.Services.Features.Items.Features.Scheduler;

public class SchedulePublishingWorker(
    IHubContext<UpdatesHub> hub,
    IServiceProvider provider, 
    ILogger<SchedulePublishingWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SchedulePublishingWorker activated.");
        
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
            logger.LogInformation("SchedulePublishingWorker deactivated.");
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

            var messages = await bus.InvokeAsync<List<string>>(new SchedulePublishingCommand(), stoppingToken);

            foreach (var message in messages)
            {
                await hub.Clients.All.SendAsync("OutboxEvent", message, stoppingToken);
            }
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