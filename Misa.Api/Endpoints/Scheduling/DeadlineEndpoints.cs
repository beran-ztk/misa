using Microsoft.AspNetCore.Mvc;
using Misa.Application.Scheduling.Commands.Deadlines;
using Misa.Contract.Scheduling;
using Wolverine;

namespace Misa.Api.Endpoints.Scheduling;

public static class DeadlineEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPut("/items/{itemId:guid}/deadline", UpsertDeadline);
        app.MapDelete("/items/{itemId:guid}/deadline", RemoveDeadline);
    }

    private static async Task<IResult> UpsertDeadline(
        [FromRoute] Guid itemId,
        [FromBody] ScheduleDeadlineDto dto,
        UpsertItemDeadlineHandler handler)
    {
        var dueAtUtc = dto.DeadlineAt.ToUniversalTime();

        await handler.Handle(new UpsertItemDeadlineCommand(itemId, dueAtUtc));
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveDeadline(
        [FromRoute] Guid itemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new RemoveItemDeadlineCommand(itemId), ct);
        return Results.NoContent();
    }
}

