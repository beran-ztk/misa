using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Authentication;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Authentication;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Infrastructure.Client;

public sealed class AuthenticationService(RemoteProxy.RemoteProxy remoteProxy) : IAuthenticationService
{
    public async Task<Result> RegisterAsync(RegisterRequestDto requestDto, CancellationToken ct = default)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, "auth/register"),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);
        
        return response;
    }

    public async Task<AuthTokenResponseDto> LoginAsync(LoginRequestDto requestDto, CancellationToken ct = default)
    {
        
        var response = await remoteProxy.SendAsync<AuthTokenResponseDto>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, "auth/login"),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);
        
        return response.Value ?? throw new Exception("Could not login");
    }
}