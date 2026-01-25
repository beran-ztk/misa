using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Wolverine;

namespace Misa.Api.Endpoints.Features.Entities.Extensions.Items.Features;

public static class SchedulingEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("scheduling", AddSchedulingRule);
    }

    private static async Task<IResult> AddSchedulingRule(
        [FromBody] AddScheduleDto dto, 
        IMessageBus bus, 
        CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            var title = string.IsNullOrWhiteSpace(dto.Title) 
                ? $"Schedule-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}" 
                : dto.Title;
                
            var command = new AddScheduleCommand(
                title,
                dto.ScheduleFrequencyType,
                dto.FrequencyInterval,
                dto.OccurrenceCountLimit,
                dto.MisfirePolicy,
                dto.OccurrenceTtl,
                dto.StartTime,
                dto.EndTime,
                dto.ActiveFromUtc,
                dto.ActiveUntilUtc
            );
            
            var result = await bus.InvokeAsync<Result>(command, linkedCts.Token);
            
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }
}