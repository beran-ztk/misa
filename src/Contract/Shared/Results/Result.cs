namespace Misa.Contract.Shared.Results;

public enum ResultStatus
{
    Success,
    Invalid,
    Conflict,
    NotFound
}

public sealed record Error(string Code, string Message);
    
public class Result
{
    public ResultStatus Status { get; init; }
    public Error? Error { get; init; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result Ok() => new() { Status = ResultStatus.Success };
    public static Result Invalid(string code, string message) => new() { Status = ResultStatus.Invalid, Error = new Error(code, message)};
    
    public static Result Conflict(string code, string message) => new() { Status = ResultStatus.Conflict, Error = new Error(code, message)};
    
    public static Result NotFound(string code, string message) => new() { Status = ResultStatus.NotFound, Error = new Error(code, message)};
}

public sealed class Result<T>
{
    public ResultStatus Status { get; init; }
    public Error? Error { get; init; }
    public T? Value { get; init; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result<T> Ok(T value) => new() { Status = ResultStatus.Success, Value = value };

    public static Result<T> Invalid(string code, string message) =>
        new() { Status = ResultStatus.Invalid, Error = new Error(code, message) };
    
    public static Result<T> Conflict(string code, string message) =>
        new() { Status = ResultStatus.Conflict, Error = new Error(code, message) };
    
    public static Result<T> NotFound(string code, string message) =>
        new() { Status = ResultStatus.NotFound, Error = new Error(code, message) };
}