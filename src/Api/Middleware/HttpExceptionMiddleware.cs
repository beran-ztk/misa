using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Misa.Domain.Exceptions;

namespace Misa.Api.Middleware;


public sealed class HttpExceptionMiddleware(ILogger<HttpExceptionMiddleware> log) : IMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected; do not attempt to write a response.
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                log.LogWarning(ex, "Unhandled exception after response started. TraceId={TraceId}", context.TraceIdentifier);
                throw;
            }

            log.LogError(ex, "Unhandled exception. TraceId={TraceId}", context.TraceIdentifier);

            var (statusCode, payload) = MapToProblemDetails(context, ex);

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }
    }

    private static (int statusCode, ProblemDetails payload) MapToProblemDetails(HttpContext ctx, Exception ex)
    {
        var traceId = ctx.TraceIdentifier;

        switch (ex)
        {
            case DomainValidationException v:
            {
                var vp = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [v.Field] = [v.Message]
                })
                {
                    Title = v.Code,
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://httpstatuses.com/400",
                    Detail = "Validation failed.",
                    Extensions =
                    {
                        ["traceId"] = traceId
                    }
                };

                return (vp.Status!.Value, vp);
            }

            case DomainNotFoundException nf:
            {
                var pd = new ProblemDetails
                {
                    Title = nf.Code,
                    Detail = nf.Message,
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://httpstatuses.com/404",
                    Extensions =
                    {
                        ["traceId"] = traceId
                    }
                };
                return (pd.Status!.Value, pd);
            }

            case DomainConflictException cf:
            {
                var pd = new ProblemDetails
                {
                    Title = cf.Code,
                    Detail = cf.Message,
                    Status = StatusCodes.Status409Conflict,
                    Type = "https://httpstatuses.com/409",
                    Extensions =
                    {
                        ["traceId"] = traceId
                    }
                };
                return (pd.Status!.Value, pd);
            }

            case TimeoutException:
            {
                var pd = new ProblemDetails
                {
                    Title = "timeout",
                    Detail = "The request took too long to complete.",
                    Status = StatusCodes.Status408RequestTimeout,
                    Type = "https://httpstatuses.com/408",
                    Extensions =
                    {
                        ["traceId"] = traceId
                    }
                };
                return (pd.Status!.Value, pd);
            }

            default:
            {
                var pd = new ProblemDetails
                {
                    Title = "unexpected_error",
                    Detail = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://httpstatuses.com/500",
                    Extensions =
                    {
                        ["traceId"] = traceId
                    }
                };
                return (pd.Status!.Value, pd);
            }
        }
    }
}