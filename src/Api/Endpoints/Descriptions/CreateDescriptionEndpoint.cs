using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Entities.Features.Descriptions.Commands;
using Misa.Contract.Features.Entities.Features;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Descriptions;

public class CreateDescriptionEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapPost("descriptions", AddDescription);
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
}