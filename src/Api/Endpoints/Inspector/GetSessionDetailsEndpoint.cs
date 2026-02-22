using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Inspector;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Inspector;

public static class GetSessionDetailsEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch(ActivityRoutes.UpsertDeadline, UpsertDeadline);
        endpoints.MapGet("items/{itemId:guid}/details", GetItemDetails);
    }
    private static async Task<IResult> UpsertDeadline(
        [FromRoute] Guid itemId,
        [FromBody] UpsertDeadlineRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new UpsertDeadlineCommand(itemId, request.DueAtUtc), ct);

        return Results.Ok();
    }    
    private static async Task<IResult> GetItemDetails(
        [FromRoute] Guid itemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var dto = await bus.InvokeAsync<ItemDto>(new GetItemDetailsQuery(itemId), ct);

        return Results.Ok(dto);
    }    
}