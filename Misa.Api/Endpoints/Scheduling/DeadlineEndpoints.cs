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
        var utc = scheduleDto.StartAtUtc.ToUniversalTime();
        
        await handler.Handle(
            new SetEntityDeadlineCommand(scheduleDto.EntityId, utc)
        );
        
        return Results.NoContent();
    }
}