using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Schedules;

namespace Misa.Application.Features.Items.Schedules.Commands;

public record GetScheduleQuery;
public class GetSchedulesHandler(IItemRepository repository, ICurrentUser user)
{
    public async Task<Result<IReadOnlyCollection<ScheduleExtensionDto>>> HandleAsync(GetScheduleQuery query, CancellationToken ct)
    {
        var result = await repository.GetSchedulesAsync(user.Id, ct);
        
        var schedules = result.ToScheduleExtensionDto();
        return Result<IReadOnlyCollection<ScheduleExtensionDto>>.Ok(schedules);
    }
}