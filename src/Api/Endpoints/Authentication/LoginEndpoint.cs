using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Authentication;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Authentication;
using Wolverine;

namespace Misa.Api.Endpoints.Authentication;

public static class LoginEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("auth/login", Login)
            .AllowAnonymous();
    }
    
    private static async Task<IResult> Login(
        [FromBody] LoginRequestDto dto,
        IMessageBus bus,
        CancellationToken ct = default)
    {
        var cmd = new LoginCommand(dto.Username, dto.Password);
        var result = await bus.InvokeAsync<AuthTokenResponseDto>(cmd, ct);
        return Results.Ok(result);
    }
}