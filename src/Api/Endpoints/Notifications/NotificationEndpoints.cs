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
        api.MapPost(NotificationRoutes.MarkRead, MarkRead);
        api.MapPost(NotificationRoutes.MarkAllRead, MarkAllRead);
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

    private static async Task<IResult> MarkRead(Guid id, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new MarkNotificationReadCommand(id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllRead(IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new MarkAllNotificationsReadCommand(), ct);
        return Results.NoContent();
    }
}
