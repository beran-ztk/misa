using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Wolverine;

namespace Misa.Api.Endpoints.Scheduling;

public static class SchedulingEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPut("/scheduling", AddSchedulingRule);
    }

    private static async Task<IResult> AddSchedulingRule(IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new AddScheduleCommand(), ct);
        return Results.NoContent();
    }
}