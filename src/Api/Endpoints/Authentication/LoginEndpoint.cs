using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Authentication;
using Misa.Contract.Features.Authentication;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Authentication;

public static class LoginEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("auth/login", Login);
    }
    
    private static async Task<Result<AuthTokenResponseDto>> Login(
        [FromBody] LoginRequestDto dto,
        IMessageBus bus,
        CancellationToken ct = default)
    {
        var cmd = new LoginCommand(dto.Username, dto.Password);
        return await bus.InvokeAsync<Result<AuthTokenResponseDto>>(cmd, ct);
    }
}