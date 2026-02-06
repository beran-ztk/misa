using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Shared.Results;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public record GetScheduleQuery;
public class GetSchedulesHandler(IItemRepository repository)
{
    public async Task<Result<List<ScheduleDto>>> HandleAsync(GetScheduleQuery query, CancellationToken ct)
    {
        var result = await repository.GetSchedulingRulesAsync(ct);
        var schedules = result.ToDto();
        var x = Result<List<ScheduleDto>>.Ok(schedules);
        return x;
    }
}