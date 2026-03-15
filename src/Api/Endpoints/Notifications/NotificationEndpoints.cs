using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Notifications;
using Misa.Contract.Notifications;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Notifications;

public static class NotificationEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapGet(NotificationRoutes.GetAll, GetAll);
        api.MapDelete(NotificationRoutes.Dismiss, Dismiss);
    }

    private static async Task<IResult> GetAll(
        IMessageBus bus,
        CancellationToken ct,
        [FromQuery] int limit = 25,
        [FromQuery] DateTimeOffset? before = null)
    {
        var dtos = await bus.InvokeAsync<List<NotificationEntryDto>>(
            new GetNotificationsQuery(limit, before), ct);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> Dismiss(Guid id, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new DismissNotificationCommand(id), ct);
        return Results.NoContent();
    }
}
