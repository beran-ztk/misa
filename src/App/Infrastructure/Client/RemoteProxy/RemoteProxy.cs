using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

public sealed class RemoteProxy(HttpClient httpClient, UserState userState)
{
    // Add jwt token to request
    private HttpRequestMessage AddJwtToken(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(userState.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userState.Token);
        }

        return request;
    }

    // Check if request should be sent again
    private static bool ShouldRetry(HttpStatusCode statusCode, int attempt, RetryOptions retry)
    {
        if (attempt >= retry.MaxAttempts)
        {
            return false;
        }
        
        return statusCode == HttpStatusCode.RequestTimeout 
               || (int)statusCode >= 500;
    }

    // Delay before sending request again
    private static Task DelayBeforeRetryAsync(RetryOptions retry, int attempt, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(retry.Delay.TotalMilliseconds * attempt);
        return Task.Delay(delay, cancellationToken);
    }
    
    // Send request (generic)
    public async Task<Result<T>> SendAsync<T>(
        Func<HttpRequestMessage> requestFactory,
        RetryOptions? retry = null,
        CancellationToken cancellationToken = default)
    {
        retry ??= RetryOptions.None;

        for (var attempt = 1; attempt <= retry.MaxAttempts; attempt++)
        {
            try
            {
                using var request = AddJwtToken(requestFactory());
                using var response = await httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    if (!ShouldRetry(response.StatusCode, attempt, retry))
                    {
                        return await ReadFailure<T>(response);
                    }

                    await DelayBeforeRetryAsync(retry, attempt, cancellationToken);
                    continue;
                }

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return Result<T>.Ok(default!);
                }

                var dto = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

                return dto is null
                    ? RemoteProxyErrors.EmptyResponse<T>()
                    : Result<T>.Ok(dto);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt >= retry.MaxAttempts)
                {
                    return RemoteProxyErrors.Timeout<T>(ex.Message);
                }
                
                await DelayBeforeRetryAsync(retry, attempt, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // TODO: Aufrufer muss catchen
                throw;
            }
            catch (HttpRequestException ex)
            {
                if (attempt >= retry.MaxAttempts)
                {
                    return RemoteProxyErrors.HttpError<T>(ex.Message);
                }

                await DelayBeforeRetryAsync(retry, attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                return RemoteProxyErrors.Unexpected<T>(ex.Message);
            }
        }    
        
        return RemoteProxyErrors.RetryExhausted<T>();
    }

    // Send request (non-generic)
    public async Task<Result> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        RetryOptions? retry = null,
        CancellationToken cancellationToken = default)
    {
        retry ??= RetryOptions.None;

        for (var attempt = 1; attempt <= retry.MaxAttempts; attempt++)
        {
            try
            {
                using var request = AddJwtToken(requestFactory());
                using var response = await httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return Result.Ok();
                }

                if (!ShouldRetry(response.StatusCode, attempt, retry))
                {
                    return await ReadFailure(response);
                }

                await DelayBeforeRetryAsync(retry, attempt, cancellationToken);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt >= retry.MaxAttempts)
                {
                    return RemoteProxyErrors.Timeout(ex.Message);
                }

                await DelayBeforeRetryAsync(retry, attempt, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // TODO: Aufrufer muss catchen
                throw;
            }
            catch (HttpRequestException ex)
            {
                if (attempt >= retry.MaxAttempts)
                {
                    return RemoteProxyErrors.HttpError(ex.Message);
                }

                await DelayBeforeRetryAsync(retry, attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                return RemoteProxyErrors.Unexpected(ex.Message);
            }
        }

        return RemoteProxyErrors.RetryExhausted();
    }

    private static async Task<Result<T>> ReadFailure<T>(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (string.Equals(contentType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var validation = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>();
                if (validation is not null)
                {
                    var (field, message) = CombineValidationErrors(validation);
                    return Result<T>.Invalid(field, code: validation.Title ?? "validation_failed", message: message);
                }
            }
            catch (JsonException)
            {
                // ignore
            }

            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>();
                if (problem is not null)
                {
                    var code = problem.Title ?? $"http_{(int)response.StatusCode}";
                    var msg = problem.Detail ?? $"Request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
                    return Result<T>.Failure(code: code, message: msg);
                }
            }
            catch (JsonException)
            {
                // ignore
            }
        }

        // Fallback: plain text / unknown json
        var raw = await response.Content.ReadAsStringAsync();
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
            try
            {
                var validation = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>();
                if (validation is not null)
                {
                    var (field, message) = CombineValidationErrors(validation);
                    return Result.Invalid(field, code: validation.Title ?? "validation_failed", message: message);
                }
            }
            catch (JsonException)
            {
                // ignore
            }
            
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>();
                if (problem is not null)
                {
                    var code = problem.Title ?? $"http_{(int)response.StatusCode}";
                    var msg = problem.Detail ?? $"Request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
                    return Result.Failure(code: code, message: msg);
                }
            }
            catch (JsonException)
            {
                // ignore
            }
        }

        var raw = await response.Content.ReadAsStringAsync();
        return Result.Failure(
            code: $"http_{(int)response.StatusCode}",
            message: string.IsNullOrWhiteSpace(raw)
                ? $"Request failed ({(int)response.StatusCode} {response.ReasonPhrase})."
                : raw);
    }

    private static (string field, string message) CombineValidationErrors(ValidationProblemDetailsDto vpd)
    {
        if (vpd.Errors is null || vpd.Errors.Count == 0)
            return ("", vpd.Detail ?? "Validation failed.");

        var parts = new List<string>();

        foreach (var kv in vpd.Errors)
        {
            var field = kv.Key;

            foreach (var msg in kv.Value)
            {
                parts.Add($"{field}: {msg}");
            }
        }

        var combined = string.Join("; ", parts);

        return ("", combined);
    }
}