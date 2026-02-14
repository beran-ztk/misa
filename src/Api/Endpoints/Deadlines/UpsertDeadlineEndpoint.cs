using Misa.Application.Features.Common.Deadlines;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Deadlines;

public static class UpsertDeadlineEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPut("deadlines", UpsertDeadline);
    }
    private static async Task<Result> UpsertDeadline(
        UpsertDeadlineDto dto,
        IMessageBus bus,
        CancellationToken ct)
    {
        var command = new UpsertDeadlineCommand(
            dto.ItemId,
            dto.DueAtUtc
        );

        return await bus.InvokeAsync<Result>(command, ct);
    }
}