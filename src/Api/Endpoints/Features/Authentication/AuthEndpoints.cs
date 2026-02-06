using Microsoft.AspNetCore.Mvc;
using Misa.Application.Features.Authentication;
using Misa.Contract.Features.Authentication;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Features.Authentication;

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("auth/register", Register);
        app.MapPost("auth/login", Login);
    }

    private static async Task<Result<AuthResponseDto>> Register(
        [FromBody] RegisterRequestDto dto,
        IMessageBus bus,
        CancellationToken ct = default)
    {
        var cmd = new RegisterCommand(dto.Username, dto.Password, dto.TimeZone);
        return await bus.InvokeAsync<Result<AuthResponseDto>>(cmd, ct);
    }

    private static async Task<Result<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto dto,
        IMessageBus bus,
        CancellationToken ct = default)
    {
        var cmd = new LoginCommand(dto.Username, dto.Password);
        return await bus.InvokeAsync<Result<AuthResponseDto>>(cmd, ct);
    }
}