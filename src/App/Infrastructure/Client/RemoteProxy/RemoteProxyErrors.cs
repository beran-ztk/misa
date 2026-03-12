using Misa.Contract.Common.Results;

namespace Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

public static class RemoteProxyErrors
{
    // Error codes
    private const string EmptyResponseCode = "empty_response";
    private const string TimeoutCode = "timeout";
    private const string HttpErrorCode = "http_error";
    private const string UnexpectedErrorCode = "unexpected_error";
    private const string RetryExhaustedCode = "retry_exhausted";

    private const string EmptyResponseMessage = "Empty response from server.";
    private const string RetryExhaustedMessage = "Request failed after all retry attempts.";

    // Result (non-generic)
    public static Result Timeout(string details) =>
        Result.Failure(TimeoutCode, $"Request timeout: {details}");

    public static Result HttpError(string details) =>
        Result.Failure(HttpErrorCode, $"HTTP error: {details}");

    public static Result Unexpected(string details) =>
        Result.Failure(UnexpectedErrorCode, $"Unexpected error: {details}");

    public static Result RetryExhausted() =>
        Result.Failure(RetryExhaustedCode, RetryExhaustedMessage);
    
    // Result<T> (generic)
    public static Result<T> EmptyResponse<T>() =>
        Result<T>.Failure(EmptyResponseCode, EmptyResponseMessage);

    public static Result<T> Timeout<T>(string details) =>
        Result<T>.Failure(TimeoutCode, $"Request timeout: {details}");

    public static Result<T> HttpError<T>(string details) =>
        Result<T>.Failure(HttpErrorCode, $"HTTP error: {details}");

    public static Result<T> Unexpected<T>(string details) =>
        Result<T>.Failure(UnexpectedErrorCode, $"Unexpected error: {details}");

    public static Result<T> RetryExhausted<T>() =>
        Result<T>.Failure(RetryExhaustedCode, RetryExhaustedMessage);
}