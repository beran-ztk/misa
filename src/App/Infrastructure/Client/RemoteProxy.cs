using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.Client;

public sealed class RemoteProxy(HttpClient httpClient, UserState userState)
{
    private HttpRequestMessage AddJwtToken(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(userState.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userState.Token);

        return request;
    }

    public async Task<Result<T>> SendAsync<T>(HttpRequestMessage request)
    {
        try
        {
            using var response = await httpClient.SendAsync(AddJwtToken(request));

            if (!response.IsSuccessStatusCode) 
                return await ReadFailure<T>(response);
            
            if (response.StatusCode == HttpStatusCode.NoContent)
                return Result<T>.Ok(default!);

            var dto = await response.Content.ReadFromJsonAsync<T>();
            return dto is null
                ? Result<T>.Failure(code: "empty_response", message: "Empty response from server.")
                : Result<T>.Ok(dto);

        }
        catch (HttpRequestException ex)
        {
            return Result<T>.Failure(code: "http_error", message: $"HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            return Result<T>.Failure(code: "timeout", message: $"Request timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(code: "unexpected_error", message: $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result> SendAsync(HttpRequestMessage request)
    {
        try
        {
            using var response = await httpClient.SendAsync(AddJwtToken(request));

            if (response.IsSuccessStatusCode)
                return Result.Ok();

            return await ReadFailure(response);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure(code: "http_error", message: $"HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            return Result.Failure(code: "timeout", message: $"Request timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure(code: "unexpected_error", message: $"Unexpected error: {ex.Message}");
        }
    }

    private static async Task<Result<T>> ReadFailure<T>(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (string.Equals(contentType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            var validation = await TryReadValidationProblem(response);
            if (validation is not null)
            {
                var (field, message) = FirstValidationError(validation);
                return Result<T>.Invalid(field, code: validation.Title ?? "validation_failed", message: message);
            }

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>();
            if (problem is not null)
            {
                var code = problem.Title ?? $"http_{(int)response.StatusCode}";
                var msg = problem.Detail ?? $"Request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
                return Result<T>.Failure(code: code, message: msg);
            }
        }

        // Fallback: plain text / unknown json
        var raw = await SafeReadAsString(response);
        return Result<T>.Failure(
            code: $"http_{(int)response.StatusCode}",
            message: string.IsNullOrWhiteSpace(raw)
                ? $"Request failed ({(int)response.StatusCode} {response.ReasonPhrase})."
                : raw);
    }

    private static async Task<Result> ReadFailure(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (string.Equals(contentType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            var validation = await TryReadValidationProblem(response);
            if (validation is not null)
            {
                var (field, message) = FirstValidationError(validation);
                return Result.Invalid(field, code: validation.Title ?? "validation_failed", message: message);
            }

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>();
            if (problem is not null)
            {
                var code = problem.Title ?? $"http_{(int)response.StatusCode}";
                var msg = problem.Detail ?? $"Request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
                return Result.Failure(code: code, message: msg);
            }
        }

        var raw = await SafeReadAsString(response);
        return Result.Failure(
            code: $"http_{(int)response.StatusCode}",
            message: string.IsNullOrWhiteSpace(raw)
                ? $"Request failed ({(int)response.StatusCode} {response.ReasonPhrase})."
                : raw);
    }

    private static async Task<ValidationProblemDetailsDto?> TryReadValidationProblem(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>();
        }
        catch
        {
            return null;
        }
    }

    private static (string field, string message) FirstValidationError(ValidationProblemDetailsDto vpd)
    {
        if (vpd.Errors is null || vpd.Errors.Count == 0)
            return ("", vpd.Detail ?? "Validation failed.");

        var first = vpd.Errors.First();
        var field = first.Key;
        var message = first.Value.FirstOrDefault() ?? vpd.Detail ?? "Validation failed.";
        return (field, message);
    }

    private static async Task<string?> SafeReadAsString(HttpResponseMessage response)
    {
        try { return await response.Content.ReadAsStringAsync(); }
        catch { return null; }
    }
}