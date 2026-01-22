using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Queries;

public class GetCurrentSessionDetailsHandler(IItemRepository repository)
{
    public async Task<Result<CurrentSessionOverviewDto>> Handle(GetCurrentSessionDetailsQuery command, CancellationToken ct)
    {
        if (command.ItemId == Guid.Empty)
        {
            return Result<CurrentSessionOverviewDto>.Invalid(ItemErrorCodes.ItemIdEmpty, "ItemId must not be empty.");
        }

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

        dto.CanStartSession = dto.ActiveSession == null;
        dto.CanStopSession = dto.ActiveSession != null;
        dto.CanPauseSession = dto.ActiveSession?.State == nameof(SessionState.Running);
        dto.CanContinueSession = dto.ActiveSession?.State == nameof(SessionState.Paused);

        return Result<CurrentSessionOverviewDto>.Ok(dto);
    }
}