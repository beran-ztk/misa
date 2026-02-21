namespace Misa.Contract.Common.Results;

public enum ResultStatus
{
    Success,
    Invalid,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden,
    Failure
}

public sealed record Error(string Code, string Message);

public sealed record ValidationError(string Field, string Code, string Message);

public class Result
{
    public ResultStatus Status { get; init; }
    public Error? Error { get; init; }
    public IReadOnlyList<ValidationError>? ValidationErrors { get; init; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result Ok() => new() { Status = ResultStatus.Success };

    public static Result Invalid(string field, string code, string message) =>
        new()
        {
            Status = ResultStatus.Invalid,
            ValidationErrors = [new ValidationError(field, code, message)]
        };

    public static Result Invalid(params ValidationError[] errors) =>
        new()
        {
            Status = ResultStatus.Invalid,
            ValidationErrors = errors
        };

    public static Result Conflict(string code, string message) =>
        new() { Status = ResultStatus.Conflict, Error = new Error(code, message) };

    public static Result NotFound(string code, string message) =>
        new() { Status = ResultStatus.NotFound, Error = new Error(code, message) };

    public static Result Unauthorized(string code = "unauthorized", string message = "Unauthorized") =>
        new() { Status = ResultStatus.Unauthorized, Error = new Error(code, message) };

    public static Result Forbidden(string code = "forbidden", string message = "Forbidden") =>
        new() { Status = ResultStatus.Forbidden, Error = new Error(code, message) };

    public static Result Failure(string code = "failure", string message = "An unexpected error occurred") =>
        new() { Status = ResultStatus.Failure, Error = new Error(code, message) };
}

public sealed class Result<T>
{
    public ResultStatus Status { get; init; }
    public Error? Error { get; init; }
    public IReadOnlyList<ValidationError>? ValidationErrors { get; init; }
    public T? Value { get; init; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result<T> Ok(T value) =>
        new() { Status = ResultStatus.Success, Value = value };

    public static Result<T> Invalid(string field, string code, string message) =>
        new()
        {
            Status = ResultStatus.Invalid,
            ValidationErrors = [new ValidationError(field, code, message)]
        };

    public static Result<T> Invalid(params ValidationError[] errors) =>
        new()
        {
            Status = ResultStatus.Invalid,
            ValidationErrors = errors
        };

    public static Result<T> Conflict(string code, string message) =>
        new() { Status = ResultStatus.Conflict, Error = new Error(code, message) };

    public static Result<T> NotFound(string code, string message) =>
        new() { Status = ResultStatus.NotFound, Error = new Error(code, message) };

    public static Result<T> Unauthorized(string code = "unauthorized", string message = "Unauthorized") =>
        new() { Status = ResultStatus.Unauthorized, Error = new Error(code, message) };

    public static Result<T> Forbidden(string code = "forbidden", string message = "Forbidden") =>
        new() { Status = ResultStatus.Forbidden, Error = new Error(code, message) };

    public static Result<T> Failure(string code = "failure", string message = "An unexpected error occurred") =>
        new() { Status = ResultStatus.Failure, Error = new Error(code, message) };
}
