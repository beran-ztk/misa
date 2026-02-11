using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Features.Entities.Extensions.Items.Features;

public static class SchedulingEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("scheduling", GetSchedulingRules);
        app.MapPost("scheduling/{userId:guid}", AddSchedulingRule);
        
        app.MapPost("scheduling/once", CreateOnceScheduler);
        app.MapDelete("scheduling/once/{targetItemId:guid}", DeleteDeadline);
    }
    private static async Task<Result> CreateOnceScheduler(
        UpsertDeadlineDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        var command = new CreateOnceScheduleCommand(
            dto.ItemId,
            dto.DueAtUtc
        );

        return await bus.InvokeAsync<Result>(command, ct);
    }
    private static async Task<Result> DeleteDeadline(
        Guid targetItemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var command = new DeleteDeadlineCommand(targetItemId);
        return await bus.InvokeAsync<Result>(command, ct);
    }
    private static async Task<Result<List<ScheduleDto>>> GetSchedulingRules(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<Result<List<ScheduleDto>>>(new GetScheduleQuery(), ct);
        return result;
    }
    private static async Task<Result<ScheduleDto>> AddSchedulingRule(
        Guid userId,
        [FromBody] AddScheduleDto dto, 
        IMessageBus bus, 
        CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            var command = new AddScheduleCommand(
                userId,
                dto.Title,
                dto.TargetItemId,
                dto.ScheduleFrequencyType,
                dto.FrequencyInterval,
                dto.LookaheadLimit,
                dto.OccurrenceCountLimit,
                dto.MisfirePolicy,
                dto.OccurrenceTtl,
                dto.ActionType,
                dto.Payload,
                dto.StartTime,
                dto.EndTime,
                dto.ActiveFromLocal,
                dto.ActiveUntilLocal,
                dto.ByDay,
                dto.ByMonthDay,
                dto.ByMonth
            );
            
            return await bus.InvokeAsync<Result<ScheduleDto>>(command, linkedCts.Token);
        }
        catch (Exception ex)
        {
            return Result<ScheduleDto>.Conflict("", ex.Message);
        }
    }
}