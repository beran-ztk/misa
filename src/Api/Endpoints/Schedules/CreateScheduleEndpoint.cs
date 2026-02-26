using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Application.Features.Items.Schedules.Commands;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Schedules;

public static class CreateScheduleEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost(ScheduleRoutes.CreateSchedule, Create);
    }
    private static async Task<IResult> Create(
        [FromBody] CreateScheduleRequest request, 
        IMessageBus bus, 
        CancellationToken ct)
    {
        var command = new CreateScheduleCommand(
            request.Title,
            request.Description,
            request.TargetItemId,
            request.ScheduleFrequencyType,
            request.FrequencyInterval,
            request.LookaheadLimit,
            request.OccurrenceCountLimit,
            request.MisfirePolicy,
            request.OccurrenceTtl,
            request.ActionType,
            request.Payload,
            request.StartTime,
            request.EndTime,
            request.ActiveFromLocal,
            request.ActiveUntilLocal,
            request.ByDay,
            request.ByMonthDay,
            request.ByMonth
        );
            
        var dto = await bus.InvokeAsync<ScheduleDto>(command, ct);
        return Results.Ok(dto);
    }
}