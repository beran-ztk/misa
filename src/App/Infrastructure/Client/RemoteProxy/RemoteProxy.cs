using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Misa.Contract.Common.Results;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

/// <summary> HTTP transport layer for the client. </summary>
public sealed class RemoteProxy(HttpClient httpClient, ILogger<RemoteProxy> logger)
{

    public async Task<Result> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        RetryOptions? retry = null,
        CancellationToken cancellationToken = default)
    {
        var result = await SendCoreAsync<object?>(
            requestFactory,
            static (_, _) => Task.FromResult(Result<object?>.Ok(null)),
            retry ?? RetryOptions.None,
            cancellationToken);

        return result.IsSuccess ? Result.Ok() : Downcast(result);
    }
    
    public Task<Result<T>> SendAsync<T>(
        Func<HttpRequestMessage> requestFactory,
        RetryOptions? retry = null,
        CancellationToken cancellationToken = default)
        => SendCoreAsync(requestFactory, ReadJsonBodyAsync<T>, retry ?? RetryOptions.None, cancellationToken);

    // -------------------------------------------------------------------------
    // Execution pipeline
    private async Task<Result<T>> SendCoreAsync<T>(
        Func<HttpRequestMessage> requestFactory,
        Func<HttpResponseMessage, CancellationToken, Task<Result<T>>> onSuccess,
        RetryOptions retry,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= retry.MaxAttempts; attempt++)
        {
            var isLastAttempt = attempt == retry.MaxAttempts;
            HttpResponseMessage? response = null;

            try
            {
                using var request = requestFactory();
                if (!string.IsNullOrWhiteSpace(userState.Token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userState.Token);

                logger.LogDebug(
                    "HTTP {Method} {Uri} — attempt {Attempt}/{Max}",
                    request.Method, request.RequestUri, attempt, retry.MaxAttempts);

                response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogDebug(
                        "HTTP {StatusCode} — attempt {Attempt} succeeded",
                        (int)response.StatusCode, attempt);

                    return await onSuccess(response, cancellationToken);
                }

                // Non-success: retry if transient and attempts remain
                if (!isLastAttempt 
                    && (response.StatusCode == HttpStatusCode.RequestTimeout || (int)response.StatusCode >= 500))
                {
                    var delay = retry.ComputeDelay(attempt);

                    logger.LogWarning(
                        "HTTP {StatusCode} on attempt {Attempt}/{Max} — retrying in {DelayMs} ms",
                        (int)response.StatusCode, attempt, retry.MaxAttempts, (int)delay.TotalMilliseconds);

                    // Release the connection before sleeping so it is not held during the delay
                    response.Dispose();
                    response = null;

                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                // Non-retryable status or final attempt — parse and return the failure
                logger.LogWarning(
                    "HTTP {StatusCode} on attempt {Attempt}/{Max} — not retrying",
                    (int)response.StatusCode, attempt, retry.MaxAttempts);

                var failure = await HttpFailureParser.ParseAsync(response);
                return Upcast<T>(failure);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Genuine caller cancellation — propagate; do not convert to a failure result.
                logger.LogDebug("Request cancelled by caller on attempt {Attempt}", attempt);
                throw;
            }
            catch (OperationCanceledException ex)
            {
                // Internal timeout: HttpClient.Timeout elapsed or server stopped responding.
                logger.LogWarning(ex, "Request timed out — attempt {Attempt}/{Max}", attempt, retry.MaxAttempts);

                if (isLastAttempt)
                    return RemoteProxyErrors.Timeout<T>();

                await Task.Delay(retry.ComputeDelay(attempt), cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                // Transport failure: DNS, connection refused, TLS error, etc.
                logger.LogWarning(ex,
                    "Transport error on attempt {Attempt}/{Max}: {Message}",
                    attempt, retry.MaxAttempts, ex.Message);

                if (isLastAttempt)
                    return RemoteProxyErrors.TransportError<T>();

                await Task.Delay(retry.ComputeDelay(attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                // Unexpected — do not retry; preserve full context in logs.
                logger.LogError(ex, "Unexpected error on attempt {Attempt}", attempt);
                return RemoteProxyErrors.Unexpected<T>();
            }
            finally
            {
                response?.Dispose();
            }
        }

        // Reached only when MaxAttempts < 1 (misconfiguration) — the loop always returns
        // on the final attempt under valid configuration.
        return RemoteProxyErrors.RetryExhausted<T>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------
    private async Task<Result<T>> ReadJsonBodyAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.NoContent)
            return Result<T>.Ok(default!);

        try
        {
            var value = await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web), ct);

            return value is null
                ? RemoteProxyErrors.EmptyResponse<T>()
                : Result<T>.Ok(value);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize response body as {Type}", typeof(T).Name);
            return RemoteProxyErrors.MalformedResponse<T>();
        }
    }

    /// <summary>Lifts a non-generic failure into <see cref="Result{T}"/>, preserving all failure fields.</summary>
    private static Result<T> Upcast<T>(Result r) => new()
    {
        Status          = r.Status,
        Error           = r.Error,
        ValidationErrors = r.ValidationErrors
    };

    /// <summary>Drops the type parameter from a <see cref="Result{T}"/> failure into <see cref="Result"/>.</summary>
    private static Result Downcast<T>(Result<T> r) => new()
    {
        Status          = r.Status,
        Error           = r.Error,
        ValidationErrors = r.ValidationErrors
    };
}
