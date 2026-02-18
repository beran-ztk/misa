using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Features.Descriptions.Commands;
using Misa.Contract.Features.Entities.Features;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Descriptions;

public static class UpdateDescriptionEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("descriptions", UpdateDescription);
    }
    private static async Task<Result<DescriptionDto>> UpdateDescription(
        [FromBody] DescriptionUpdateDto dto,
        IMessageBus bus,
        CancellationToken ct = default)
    {
        var cmd = new UpdateDescriptionCommand(dto.Id, dto.Content);

        return await bus.InvokeAsync<Result<DescriptionDto>>(cmd, ct);
    }
}