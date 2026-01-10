using Microsoft.AspNetCore.Mvc;
using Misa.Application.Items.Queries;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Details;
using Wolverine;

namespace Misa.Api.Endpoints.Items;

public class ItemDetailEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("items/{itemId:guid}/overview", GetDetails);
    }

    private static async Task<Result<ItemOverviewDto>> GetDetails(
        [FromQuery] Guid itemId,
        IMessageBus bus, 
        CancellationToken ct)
    {
        var res = await bus.InvokeAsync<Result<ItemOverviewDto>>(new GetItemDetailsQuery(itemId), ct);
        return res;
    }
}