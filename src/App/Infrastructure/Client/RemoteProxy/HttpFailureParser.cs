using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;

namespace Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

public static class HttpFailureParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<Result> ParseAsync(HttpResponseMessage response)
    {
        var statusCode = response.StatusCode;
        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (string.Equals(contentType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            var structured = await TryParseProblemDetailsAsync(response, statusCode);
            if (structured is not null)
                return structured;
        }

        return await ParseFallbackAsync(response, statusCode);
    }

    private static async Task<Result?> TryParseProblemDetailsAsync(HttpResponseMessage response, HttpStatusCode statusCode)
    {
        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            var vpd = JsonSerializer.Deserialize<ValidationProblemDetailsDto>(body, JsonOptions);
            if (vpd?.Errors is { Count: > 0 })
                return Result.Invalid(BuildValidationErrors(vpd));
        }
        catch (JsonException) { }

        try
        {
            var pd = JsonSerializer.Deserialize<ProblemDetailsDto>(body, JsonOptions);
            if (pd is not null)
                return MapStatusCode(statusCode, pd.Title, pd.Detail);
        }
        catch (JsonException) { }

        return null;
    }

    private static async Task<Result> ParseFallbackAsync(HttpResponseMessage response, HttpStatusCode statusCode)
    {
        string? body = null;
        try
        {
            var raw = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(raw))
                body = raw;
        }
        catch
        {
            // ignored
        }

        return MapStatusCode(statusCode, title: null, detail: body);
    }

    private static Result MapStatusCode(HttpStatusCode statusCode, string? title, string? detail)
    {
        var code    = title  ?? $"http_{(int)statusCode}";
        var message = detail ?? $"Request failed ({(int)statusCode} {statusCode}).";

        return statusCode switch
        {
            HttpStatusCode.Unauthorized        => Result.Unauthorized(code, message),
            HttpStatusCode.Forbidden           => Result.Forbidden(code, message),
            HttpStatusCode.NotFound            => Result.NotFound(code, message),
            HttpStatusCode.Conflict            => Result.Conflict(code, message),
            HttpStatusCode.UnprocessableEntity => Result.Invalid(new ValidationError("", code, message)),
            _                                  => Result.Failure(code, message)
        };
    }

    private static ValidationError[] BuildValidationErrors(ValidationProblemDetailsDto vpd)
    {
        var errors = new List<ValidationError>();

        foreach (var (field, messages) in vpd.Errors!)
        foreach (var msg in messages)
            errors.Add(new ValidationError(field, vpd.Title ?? "validation_failed", msg));

        return errors.Count > 0
            ? errors.ToArray()
            : [new ValidationError("", vpd.Title ?? "validation_failed", vpd.Detail ?? "Validation failed.")];
    }
}
