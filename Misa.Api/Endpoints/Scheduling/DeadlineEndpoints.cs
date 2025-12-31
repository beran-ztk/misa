using Microsoft.AspNetCore.Mvc;
using Misa.Application.Scheduling.Commands.SetEntityDeadline;
using Misa.Contract.Scheduling;

namespace Misa.Api.Endpoints.Scheduling;

public static class DeadlineEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPut("/entities/deadline", SetDeadline);
    }

    private static async Task<IResult> SetDeadline(
        [FromQuery] Guid entityId,
        [FromQuery] DateTimeOffset deadline,
        SetEntityDeadlineHandler handler)
    {
        var utc = deadline.ToUniversalTime();

        await handler.Handle(
            new SetEntityDeadlineCommand(entityId, utc)
        );

        return Results.NoContent();
    }
}
