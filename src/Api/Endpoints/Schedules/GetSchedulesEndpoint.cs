using Misa.Application.Features.Items.Schedules.Commands;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Schedules;
using Wolverine;

namespace Misa.Api.Endpoints.Schedules;

public static class GetSchedulesEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapGet("scheduling", GetSchedulingRules);
    }
    
    private static async Task<Result<IReadOnlyCollection<ScheduleExtensionDto>>> GetSchedulingRules(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<Result<IReadOnlyCollection<ScheduleExtensionDto>>>(new GetScheduleQuery(), ct);
        return result;
    }
}