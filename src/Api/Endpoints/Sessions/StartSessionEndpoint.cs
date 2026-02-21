using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity.Sessions;
using Wolverine;

namespace Misa.Api.Endpoints.Sessions;

public static class StartSessionEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost("items/{itemId:guid}/sessions/start", StartSession);
    }

    private static async Task<Result<SessionDto>> StartSession(
        [FromRoute] Guid itemId,
        [FromBody] StartSessionDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new StartSessionCommand(
            dto.ItemId,
            dto.PlannedDuration,
            dto.Objective,
            dto.StopAutomatically,
            dto.AutoStopReason
        );

        var res = await bus.InvokeAsync<Result<SessionDto>>(cmd, ct);
        return res;
    }
}