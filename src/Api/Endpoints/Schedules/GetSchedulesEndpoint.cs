using Misa.Application.Features.Items.Schedules.Commands;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Routes;
using Wolverine;

namespace Misa.Api.Endpoints.Schedules;

public static class GetSchedulesEndpoint
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapGet(ScheduleRoutes.GetSchedules, GetSchedulingRules);
    }
    
    private static async Task<IResult> GetSchedulingRules(IMessageBus bus, CancellationToken ct)
    {
        var dto = await bus.InvokeAsync<List<ScheduleDto>>(new GetScheduleQuery(), ct);
        return Results.Ok(dto);
    }
}