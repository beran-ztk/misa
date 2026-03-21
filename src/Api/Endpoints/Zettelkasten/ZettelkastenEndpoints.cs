using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Zettelkasten;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Zettelkasten;

public static class ZettelkastenEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost(ZettelkastenRoutes.CreateTopic, CreateTopic);
        api.MapGet(ZettelkastenRoutes.GetKnowledgeIndex, GetZettelkasten);
        
        api.MapPost(ZettelkastenRoutes.CreateZettel, CreateZettel);
        api.MapGet(ZettelkastenRoutes.GetZettel, GetSingle);
        api.MapPatch(ZettelkastenRoutes.UpdateZettelContent, UpdateContent);
    }

    private static async Task<IResult> GetZettelkasten(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<List<KnowledgeIndexEntryDto>>(new GetKnowledgeIndexQuery(), ct);
        var httpResponse = Results.Ok(result);
        return httpResponse;
    }
    private static async Task<IResult> CreateTopic(
        [FromBody] CreateTopicRequest request,
        IMessageBus bus)
    {
        var command = new CreateTopicCommand(request.Title, request.ParentId);
        await bus.InvokeAsync(command);
        return Results.Ok();
    }
    
    private static async Task<IResult> CreateZettel(
        [FromBody] CreateZettelRequest request,
        IMessageBus bus)
    {
        var command = new CreateZettelCommand(request.Title, request.ParentId);
        await bus.InvokeAsync(command);
        return Results.Ok();
    }

    private static async Task<IResult> GetSingle(
        Guid itemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<ZettelDto?>(new GetZettelQuery(itemId), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateContent(
        Guid itemId,
        [FromBody] UpdateZettelContentRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new UpdateZettelContentCommand(itemId, request.Content), ct);
        return Results.Ok();
    }
}
