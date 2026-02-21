using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.Client;

public class RemoteProxy(HttpClient httpClient, UserState userState)
{
    private HttpRequestMessage AddJwtToken(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userState.Token);
        return request;
    }
    public async Task<Result<T>> SendAsync<T>(HttpRequestMessage request)
    {
        try
        {
            using var response = await httpClient.SendAsync(AddJwtToken(request));

            var result = await response.Content.ReadFromJsonAsync<Result<T>>();

            return result ?? Result<T>.Failure(message: "Empty response from server.");
        }
        catch (HttpRequestException ex)
        {
            return Result<T>.Failure(message: $"HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            return Result<T>.Failure(message: $"Request timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(message: $"Unexpected error: {ex.Message}");
        }
    }
    public async Task<Result> SendAsync(HttpRequestMessage request)
    {
        try
        {
            using var response = await httpClient.SendAsync(AddJwtToken(request));

            var result = await response.Content.ReadFromJsonAsync<Result>();

            return result ?? Result.Failure(message: "Empty response from server.");
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure(message: $"HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            return Result.Failure(message: $"Request timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure(message: $"Unexpected error: {ex.Message}");
        }
    }
}