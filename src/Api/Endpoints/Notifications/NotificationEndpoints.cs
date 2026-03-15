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
    }

    private static async Task<IResult> GetAll(IMessageBus bus, CancellationToken ct)
    {
        var dtos = await bus.InvokeAsync<List<NotificationEntryDto>>(new GetNotificationsQuery(), ct);
        return Results.Ok(dtos);
    }
}
