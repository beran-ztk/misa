using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Queries;
public record GetCurrentSessionDetailsQuery(Guid ItemId);
public class GetCurrentSessionDetailsHandler(IItemRepository repository, ITimeProvider timeProvider) 
{
    public async Task<Result<CurrentSessionOverviewDto>> Handle(GetCurrentSessionDetailsQuery command, CancellationToken ct)
    {
        var item = await repository.TryGetItemAsync(command.ItemId, ct);
        if (item is null)
        {
            return Result<CurrentSessionOverviewDto>.NotFound(ItemErrorCodes.ItemNotFound, "Item not found.");
        }

        var dto = new CurrentSessionOverviewDto();
        
        var latestCompletedSession = await repository.TryGetLatestCompletedSessionByItemIdAsync(command.ItemId, ct);
        if (latestCompletedSession != null)
            dto.LatestClosedSession = latestCompletedSession.ToDto();
        
        var activeSession = await repository.TryGetActiveSessionByItemIdAsync(command.ItemId, ct);
        if (activeSession != null)
            dto.ActiveSession = activeSession.ToDto();

        return Result<CurrentSessionOverviewDto>.Ok(dto);
    }
}