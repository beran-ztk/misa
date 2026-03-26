using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Authentication;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Authentication;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Shell.Authentication;

public sealed class AuthenticationGateway(RemoteProxy remoteProxy)
{
    public async Task<Result> RegisterAsync(RegisterRequestDto requestDto)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, "auth/register")
            {
                Content = JsonContent.Create(requestDto)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<AuthTokenResponseDto> LoginAsync(LoginRequestDto requestDto)
    {
        var response = await remoteProxy.SendAsync<AuthTokenResponseDto>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, "auth/login")
            {
                Content = JsonContent.Create(requestDto)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);

        return response.Value ?? throw new InvalidOperationException("Login succeeded but returned no token.");
    }
}
