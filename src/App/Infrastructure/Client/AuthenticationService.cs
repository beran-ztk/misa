using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Features.Authentication;
using Misa.Contract.Shared.Results;

namespace Misa.Ui.Avalonia.Infrastructure.Client;

public sealed class AuthenticationService(HttpClient httpClient) : IAuthenticationService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto requestDto, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,"auth/register");
        request.Content = JsonContent.Create(requestDto);
        
        var response = await httpClient
            .SendAsync(request, CancellationToken.None);

        if (!response.IsSuccessStatusCode)
            throw await CreateError(response, ct);

        var payload = await response.Content.ReadFromJsonAsync<Result<AuthResponseDto>>(cancellationToken: ct)
                      ?? throw new InvalidOperationException("Empty response.");

        return payload.Value ?? throw new InvalidOperationException("Empty User-Data.");
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto requestDto, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,"auth/login");
        request.Content = JsonContent.Create(requestDto);
        
        var response = await httpClient
            .SendAsync(request, CancellationToken.None);

        var payload = await response.Content.ReadFromJsonAsync<Result<AuthResponseDto>>(cancellationToken: ct)
                      ?? throw new InvalidOperationException("Empty response.");

        return payload.Value ?? throw new InvalidOperationException("Empty User-Data.");
    }
    private static async Task<Exception> CreateError(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        var msg = string.IsNullOrWhiteSpace(body)
            ? $"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}"
            : body;

        return new InvalidOperationException(msg);
    }
}