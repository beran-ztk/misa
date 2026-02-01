using Misa.Contract.Common.Results;

namespace Misa.Api.Common;

public static class ResultHttpMapper
{
    public static IResult ToIResult(this Result result)
    {
        return result.Status switch
        {
            ResultStatus.Success => Results.NoContent(),

            ResultStatus.Invalid => Results.BadRequest(new
            {
                code = result.Error?.Code,
                message = result.Error?.Message
            }),

            ResultStatus.Conflict => Results.Conflict(new
            {
                code = result.Error?.Code,
                message = result.Error?.Message
            }),
            
            ResultStatus.NotFound => Results.NotFound(new
            {
                code = result.Error?.Code,
                message = result.Error?.Message
            }),
            
            _ => Results.Problem("Unexpected result status.")
        };
    }

    public static IResult ToIResult<T>(this Result<T> result)
    {
        return result.Status switch
        {
            ResultStatus.Success => Results.Ok(result.Value),

            ResultStatus.Invalid => Results.BadRequest(new
            {
                code = result.Error?.Code,
                message = result.Error?.Message
            }),

            ResultStatus.Conflict => Results.Conflict(new
            {
                code = result.Error?.Code,
                message = result.Error?.Message
            }),

            ResultStatus.NotFound => Results.NotFound(new
            {
                code = result.Error?.Code,
                message = result.Error?.Message
            }),

            _ => Results.Problem("Unexpected result status.")
        };
    }
}