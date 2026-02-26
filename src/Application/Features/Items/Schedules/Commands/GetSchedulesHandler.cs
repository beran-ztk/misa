using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Schedules;

namespace Misa.Application.Features.Items.Schedules.Commands;

public record GetScheduleQuery;
public class GetSchedulesHandler(IItemRepository repository)
{
    public async Task<List<ScheduleDto>> HandleAsync(GetScheduleQuery query, CancellationToken ct)
    {
        var result = await repository.GetSchedulesAsync(ct);
        
        var schedules = result.ToScheduleExtensionDto();
        return schedules;
    }
}