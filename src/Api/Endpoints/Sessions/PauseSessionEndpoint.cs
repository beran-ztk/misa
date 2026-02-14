using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Sessions;

public static class PauseSessionEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost("items/{itemId:guid}/sessions/pause", PauseSession);
    }

    private static async Task<Result<SessionResolvedDto>> PauseSession(
        [FromRoute] Guid itemId,
        [FromBody] PauseSessionDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new PauseSessionCommand(
            dto.ItemId,
            dto.PauseReason
        );

        var res = await bus.InvokeAsync<Result<SessionResolvedDto>>(cmd, ct);
        return res;
    }
}