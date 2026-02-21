using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Shared.Results;

namespace Misa.Application.Features.Items.Schedules.Commands;

public record GetScheduleQuery;
public class GetSchedulesHandler(IItemRepository repository)
{
    public async Task<Result<IReadOnlyCollection<ScheduleExtensionDto>>> HandleAsync(GetScheduleQuery query, CancellationToken ct)
    {
        var result = await repository.GetSchedulingRulesAsync(ct);
        
        var schedules = result.ToScheduleExtensionDto();
        return Result<IReadOnlyCollection<ScheduleExtensionDto>>.Ok(schedules);
    }
}