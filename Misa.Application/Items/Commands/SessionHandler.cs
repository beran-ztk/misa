using Misa.Application.Common.Abstractions.Persistence;
using Misa.Contract.Audit;
using Misa.Contract.Audit.Session;
using Misa.Domain.Audit;

namespace Misa.Application.Items.Commands;

public class SessionHandler(IItemRepository repository, IEntityRepository entityRepository)
{
    public async Task PauseSessionAsync(PauseSessionDto dto)
    {
        var entity = await entityRepository.GetDetailedEntityAsync(dto.ItemId);
        if (entity == null)
            return;

        // var latestSession = entity.GetLatestSession();
        // if (latestSession == null)
        //     return;
        //
        // latestSession.PauseSession();
        //
        // var latestSegment = latestSession.GetLatestSegment();
        // if (latestSegment == null)
        //     return;
        // latestSegment.CloseSegment(dto.PauseReason, DateTimeOffset.UtcNow);
        
        entity.Update();
        await repository.SaveChangesAsync();
    }
    public async Task StopSessionAsync(StopSessionDto dto)
    {
        var entity = await entityRepository.GetDetailedEntityAsync(dto.EntityId);
        if (entity == null)
            return;

        // entity.EndSession(dto);
        entity.Update();
        await repository.SaveChangesAsync();
    }

    public async Task ContinueSessionAsync(Guid id)
    {
        var entity = await entityRepository.GetDetailedEntityAsync(id);
        if (entity == null)
            return;

        // var latestSession = entity.GetLatestSession();
        // if (latestSession == null)
        //     return;
        //
        // latestSession.ContinueSession();
        // entity.Update();
        // latestSession.AddSegment(id, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync();
    }
}