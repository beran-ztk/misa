using Misa.Application.Features.Common.Deadlines;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Deadlines;

public static class DeleteDeadlineEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapDelete("deadlines/{targetItemId:guid}", DeleteDeadline);
    }
    private static async Task<Result> DeleteDeadline(
        Guid targetItemId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var command = new DeleteDeadlineCommand(targetItemId);
        
        return await bus.InvokeAsync<Result>(command, ct);
    }
}