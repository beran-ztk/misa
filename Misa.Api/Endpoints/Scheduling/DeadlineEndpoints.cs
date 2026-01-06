using Microsoft.AspNetCore.Mvc;
using Misa.Api.Common;
using Misa.Application.Common.Results;
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
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new UpsertItemDeadlineCommand(itemId, dto.DeadlineAt), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveDeadline(
        [FromRoute] Guid itemId,
        IMessageBus bus,
        CancellationToken userCt)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCt, timeoutCts.Token);

        try
        {
            var result = await bus.InvokeAsync<Result>(new RemoveItemDeadlineCommand(itemId), linkedCts.Token);

            return result.ToIResult();
        }
        catch (OperationCanceledException) when (userCt.IsCancellationRequested)
        {
            return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            return Results.Problem(
                title: "Request timed out", 
                statusCode: StatusCodes.Status504GatewayTimeout
            );
        }
    }
}

