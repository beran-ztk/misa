using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Sessions;

public static class StopSessionEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost("items/{itemId:guid}/sessions/stop", StopSession);
    }

    private static async Task<Result> StopSession(
        [FromRoute] Guid itemId,
        [FromBody] StopSessionDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new StopSessionCommand(
            dto.ItemId,
            dto.Efficiency,
            dto.Concentration,
            dto.Summary
        );

        var res = await bus.InvokeAsync<Result>(cmd, ct);
        return res;
    }
}