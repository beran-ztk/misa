using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Schedules.Commands;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Schedules;

public static class UpdateScheduleEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPut(ScheduleRoutes.UpdateSchedule, Update);
    }

    private static async Task<IResult> Update(
        [FromRoute] Guid itemId,
        [FromBody] UpdateScheduleRequest request,
        IMessageBus bus)
    {
        var command = new UpdateScheduleCommand(
            itemId,
            request.Title,
            request.Description,
            request.MisfirePolicy,
            request.LookaheadLimit,
            request.OccurrenceCountLimit,
            request.StartTime,
            request.EndTime,
            request.ActiveUntilUtc);

        await bus.InvokeAsync(command);
        return Results.Ok();
    }
}
