using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Items.Sessions.Queries;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Inspector;

public static class GetSessionDetailsEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("items/{itemId:guid}/overview/session", GetCurrentSessionDetails);
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