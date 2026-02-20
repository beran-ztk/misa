using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Schedules;

public static class CreateScheduleEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost("scheduling/{userId:guid}", AddSchedulingRule);
    }
    private static async Task<Result<ScheduleDto>> AddSchedulingRule(
        Guid userId,
        [FromBody] AddScheduleDto dto, 
        IMessageBus bus, 
        CancellationToken ct)
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
            
        var result = await bus.InvokeAsync<Result<ScheduleDto>>(command, ct);
        return result;
    }
}