using Misa.Contract.Common.Results;

namespace Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

/// <summary>
/// Transport-layer failure results produced by <see cref="RemoteProxy"/>.
/// These represent conditions that prevent a valid server response from being returned —
/// they are distinct from application-level failures (HTTP 4xx/5xx) parsed by HttpFailureParser.
/// </summary>
public static class RemoteProxyErrors
{
    public static Result<T> EmptyResponse<T>() =>
        Result<T>.Failure("transport_empty_response",
            "The server returned a success status but an empty response body.");

    public static Result<T> MalformedResponse<T>() =>
        Result<T>.Failure("transport_malformed_response",
            "The server returned a response that could not be deserialized.");

    public static Result<T> Timeout<T>() =>
        Result<T>.Failure("transport_timeout",
            "The request did not complete within the allowed time.");

    public static Result<T> TransportError<T>() =>
        Result<T>.Failure("transport_error",
            "A network error prevented the request from completing.");

    public static Result<T> Unexpected<T>() =>
        Result<T>.Failure("transport_unexpected",
            "An unexpected error occurred while sending the request.");

    public static Result<T> RetryExhausted<T>() =>
        Result<T>.Failure("transport_retry_exhausted",
            "The request failed after exhausting all retry attempts.");
}
