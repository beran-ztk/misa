using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Contract.Common.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Sessions;

public static class ContinueSessionEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("items/{itemId:guid}/sessions/continue", ContinueSession);
    }
    private static async Task<Result> ContinueSession(
        [FromRoute] Guid itemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var cmd = new ContinueSessionCommand(itemId);

        var res = await bus.InvokeAsync<Result>(cmd, ct);
        return res;
    }
}
