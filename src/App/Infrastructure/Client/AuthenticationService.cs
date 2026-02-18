using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Features.Authentication;

namespace Misa.Ui.Avalonia.Infrastructure.Client;

public sealed class AuthenticationService(RemoteProxy remoteProxy) : IAuthenticationService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto requestDto, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/register")
        {
            Content = JsonContent.Create(requestDto)
        };

        var result = await remoteProxy.SendAsync<AuthResponseDto>(request);

        // Falls dein Result<T> anders heißt: hier anpassen
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error?.Message ?? "Register failed.");

        return result.Value
               ?? throw new InvalidOperationException("Empty User-Data.");
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto requestDto, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/login")
        {
            Content = JsonContent.Create(requestDto)
        };

        var result = await remoteProxy.SendAsync<AuthResponseDto>(request);

        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error?.Message ?? "Login failed.");

        return result.Value
               ?? throw new InvalidOperationException("Empty User-Data.");
    }
}