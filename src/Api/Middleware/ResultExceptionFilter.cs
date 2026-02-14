using Misa.Contract.Shared.Results;
using Misa.Domain.Exceptions;

namespace Misa.Api.Middleware;

public sealed class ResultExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception ex)
        {
            // log here

            return ex switch
            {
                DomainValidationException v =>
                    Result.Invalid(v.Field, v.Code, v.Message),

                DomainNotFoundException nf =>
                    Result.NotFound(nf.Code, nf.Message),

                DomainConflictException cf =>
                    Result.Conflict(cf.Code, cf.Message),

                OperationCanceledException =>
                    Result.Conflict("timeout", "Request timed out."),

                _ =>
                    Result.Failure("unexpected_error", "An unexpected error occurred.")
            };
        }
    }
}