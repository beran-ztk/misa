using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity.Sessions;
using Wolverine;

namespace Misa.Api.Endpoints.Sessions;

public static class PauseSessionEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost("items/{itemId:guid}/sessions/pause", PauseSession);
    }

    private static async Task<Result<SessionDto>> PauseSession(
        [FromRoute] Guid itemId,
        [FromBody] PauseSessionDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new PauseSessionCommand(
            dto.ItemId,
            dto.PauseReason
        );

        var res = await bus.InvokeAsync<Result<SessionDto>>(cmd, ct);
        return res;
    }
}