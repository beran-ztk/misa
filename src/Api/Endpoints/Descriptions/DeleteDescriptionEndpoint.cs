using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Features.Descriptions.Commands;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Descriptions;

public static class DeleteDescriptionEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("descriptions/{descriptionId:guid}", DeleteDescription);
    }
    
    private static async Task<Result> DeleteDescription(
        [FromRoute] Guid descriptionId,
        IMessageBus bus,
        CancellationToken ct = default)
    {
        var cmd = new DeleteDescriptionCommand(descriptionId);
        return await bus.InvokeAsync<Result>(cmd, ct);
    }
}