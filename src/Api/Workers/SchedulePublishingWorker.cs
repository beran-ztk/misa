using Microsoft.AspNetCore.SignalR;
using Misa.Api.Middleware;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Wolverine;
using Misa.Contract.Features.Messaging;

namespace Misa.Api.Services.Features.Items.Features.Scheduler;

public class SchedulePublishingWorker(
    IHubContext<EventHub> hub,
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
            logger.LogInformation("Schedule executing worker running at.");

            // var scope = provider.CreateScope();
            // var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            //
            // var notifications = await bus.InvokeAsync<List<NotificationDto>>(new SchedulePublishingCommand(), stoppingToken);
            //
            // foreach (var notification in notifications)
            // {
            //     await hub.Clients.All.SendAsync(
            //         nameof(PublisherDto.Scheduler), 
            //         notification, 
            //         stoppingToken);
            // }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schedule executing worker failed at.");
        }
        finally
        {
            logger.LogInformation("Schedule executing worker finished at.");
        }
    }
}