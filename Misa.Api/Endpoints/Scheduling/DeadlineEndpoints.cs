using Misa.Application.Scheduling.Commands.SetEntityDeadline;
using Misa.Contract.Scheduling;

namespace Misa.Api.Endpoints.Scheduling;

public static class DeadlineEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPut("/entities/deadline", SetDeadline);
    }

    private static async Task<IResult> SetDeadline(ScheduleDto scheduleDto, SetEntityDeadlineHandler handler)
    {
        await handler.Handle(
            new SetEntityDeadlineCommand(scheduleDto.EntityId, scheduleDto.StartAtUtc)
        );
        
        return Results.NoContent();
    }
}