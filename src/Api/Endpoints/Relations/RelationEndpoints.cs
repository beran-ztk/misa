using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Relations;
using Misa.Contract.Items.Components.Relations;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Relations;

public static class RelationEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost(RelationRoutes.CreateRelation, Create);
        api.MapGet(RelationRoutes.GetRelationsForItem, GetForItem);
        api.MapGet(RelationRoutes.GetItemsForLookup, GetLookup);
        api.MapPut(RelationRoutes.UpdateRelation, Update);
        api.MapDelete(RelationRoutes.DeleteRelation, Delete);
    }

    private static async Task<IResult> Create(
        [FromBody] CreateRelationRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new CreateRelationCommand(request.SourceItemId, request.TargetItemId, request.RelationType), ct);
        return Results.Ok();
    }

    private static async Task<IResult> GetForItem(
        [FromRoute] Guid itemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<List<ItemRelationDto>>(new GetRelationsForItemQuery(itemId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetLookup(
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<List<ItemLookupDto>>(new GetItemsForLookupQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> Update(
        [FromRoute] Guid relationId,
        [FromBody] UpdateRelationRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new UpdateRelationCommand(relationId, request.RelationType), ct);
        return Results.Ok();
    }

    private static async Task<IResult> Delete(
        [FromRoute] Guid relationId,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new DeleteRelationCommand(relationId), ct);
        return Results.NoContent();
    }
}
