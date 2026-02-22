using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Inspector;
using Misa.Application.Features.Items.Sessions.Queries;
using Misa.Contract.Common.Results;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Inspector;

public static class GetSessionDetailsEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("items/{itemId:guid}/details", GetItemDetails);
        endpoints.MapGet("items/{itemId:guid}/overview/session", GetCurrentSessionDetails);
        endpoints.MapPatch(ActivityRoutes.UpsertDeadline, UpsertDeadline);
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
    private static async Task<Result<CurrentSessionOverviewDto>> GetCurrentSessionDetails(
        [FromRoute] Guid itemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var res = await bus.InvokeAsync<Result<CurrentSessionOverviewDto>>(
            new GetCurrentSessionDetailsQuery(itemId), ct);

        return res;
    }
}