using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Authentication;
using Misa.Contract.Features.Authentication;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Authentication;

public static class RegisterEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("auth/register", Register)
            .AllowAnonymous();
    }
    private static async Task<Result<AuthResponseDto>> Register(
        [FromBody] RegisterRequestDto dto,
        IMessageBus bus,
        CancellationToken ct = default)
    {
        var cmd = new RegisterCommand(dto.Email, dto.Username, dto.Password, dto.TimeZone);
        return await bus.InvokeAsync<Result<AuthResponseDto>>(cmd, ct);
    }
}