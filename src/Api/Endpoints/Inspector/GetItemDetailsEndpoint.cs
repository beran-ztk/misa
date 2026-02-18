using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Base.Queries;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Inspector;

public static class GetItemDetailsEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("items/{itemId:guid}/details", GetDetails);
    }
    private static async Task<Result<DetailedItemDto>> GetDetails(
        [FromRoute] Guid itemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var response = await bus.InvokeAsync<Result<DetailedItemDto>>(new GetItemDetailsQuery(itemId), ct);
        return response;
    }
}