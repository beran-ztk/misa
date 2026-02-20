using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Shared.Results;
using Wolverine;

namespace Misa.Api.Endpoints.Schedules;

public static class GetSchedulesEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapGet("scheduling", GetSchedulingRules);
    }
    
    private static async Task<Result<List<ScheduleDto>>> GetSchedulingRules(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<Result<List<ScheduleDto>>>(new GetScheduleQuery(), ct);
        return result;
    }
}