using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Zettelkasten;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Zettelkasten;

public class ZettelEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost(ZettelkastenRoutes.CreateZettel, Create);
        api.MapGet(ZettelkastenRoutes.GetZettels, GetAll);
    }

    private static async Task<IResult> Create(
        [FromBody] CreateZettelRequest request,
        IMessageBus bus)
    {
        var command = new CreateZettelCommand(request.Title, request.Content, request.TopicId);
        await bus.InvokeAsync(command);
        return Results.Ok();
    }

    private static async Task<IResult> GetAll(
        [FromQuery] Guid? topicId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<List<ZettelDto>>(new GetZettelsQuery(topicId), ct);
        return Results.Ok(result);
    }
}
