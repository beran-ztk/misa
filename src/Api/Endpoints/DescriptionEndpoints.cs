using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Features.Descriptions.Commands;
using Misa.Contract.Features.Entities.Features;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints;

public static class DescriptionEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("entities/description", AddDescription);
        app.MapPut("entities/description", UpdateDescription);
        app.MapDelete("entities/description/{descriptionId:guid}", DeleteDescription);
    }

    private static async Task<Result<DescriptionDto>> AddDescription(
        [FromBody] DescriptionCreateDto dto,
        IMessageBus bus,
        CancellationToken ct = default)
    {
        var cmd = new AddDescriptionCommand(dto.EntityId, dto.Content);
        
        var res = await bus.InvokeAsync<Result<DescriptionDto>>(cmd, ct);
        
        return res;
    }
    private static async Task<Result<DescriptionDto>> UpdateDescription(
        [FromBody] DescriptionUpdateDto dto,
        IMessageBus bus,
        CancellationToken ct = default)
    {
        var cmd = new UpdateDescriptionCommand(dto.Id, dto.Content);

        return await bus.InvokeAsync<Result<DescriptionDto>>(cmd, ct);
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