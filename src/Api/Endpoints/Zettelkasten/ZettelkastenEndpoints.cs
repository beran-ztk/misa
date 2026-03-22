using System.Collections.Generic;
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
        api.MapGet(ZettelkastenRoutes.GetKnowledgeIndex,        GetZettelkasten);
        api.MapGet(ZettelkastenRoutes.GetDeletedKnowledgeIndex, GetDeletedKnowledgeIndex);
        
        api.MapPost(ZettelkastenRoutes.CreateZettel, CreateZettel);
        api.MapGet(ZettelkastenRoutes.GetZettel, GetSingle);
        api.MapPatch(ZettelkastenRoutes.UpdateZettelContent, UpdateContent);
        api.MapPatch(ZettelkastenRoutes.SetKnowledgeIndexExpanded, SetExpanded);
        api.MapPatch(ZettelkastenRoutes.ReparentKnowledgeItem,    Reparent);
    }

    private static async Task<IResult> GetZettelkasten(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<List<KnowledgeIndexEntryDto>>(new GetKnowledgeIndexQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDeletedKnowledgeIndex(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<List<DeletedKnowledgeEntryDto>>(new GetDeletedKnowledgeIndexQuery(), ct);
        return Results.Ok(result);
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

    private static async Task<IResult> SetExpanded(
        Guid itemId,
        [FromBody] SetKnowledgeIndexExpandedStateRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new SetKnowledgeIndexExpandedStateCommand(itemId, request.IsExpanded), ct);
        return Results.Ok();
    }

    private static async Task<IResult> Reparent(
        Guid itemId,
        [FromBody] ReparentKnowledgeItemRequest request,
        IMessageBus bus)
    {
        await bus.InvokeAsync(new ReparentKnowledgeItemCommand(itemId, request.NewParentId));
        return Results.Ok();
    }
}
