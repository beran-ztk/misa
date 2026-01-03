using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Entities.Repositories;
using Misa.Contract.Audit;
using Misa.Domain.Audit;

namespace Misa.Application.Items.Commands;

public class SessionHandler(IItemRepository repository, IEntityRepository entityRepository)
{
    public async Task StartSessionAsync(SessionDto dto)
    {
        var hasBeenChanged = false;
        var item = await repository.GetTrackedItemAsync(dto.EntityId);
        item.StartSession(ref hasBeenChanged);

        var session = Session.Start
        (
            dto.EntityId, dto.PlannedDuration, dto.Objective, 
            dto.StopAutomatically, dto.AutoStopReason, DateTimeOffset.UtcNow
        );
        
        if (!hasBeenChanged)
            return;
        
        item.Entity.Update();
        var loadedSession = await repository.AddSessionAsync(session);

        var segment = new SessionSegment(loadedSession.Id, DateTimeOffset.UtcNow);
        
        await repository.AddAsync(segment);
    }
    public async Task PauseSessionAsync(PauseSessionDto dto)
    {
        var entity = await entityRepository.GetDetailedEntityAsync(dto.EntityId);
        if (entity == null)
            return;

        var latestSession = entity.GetLatestSession();
        if (latestSession == null)
            return;
        
        latestSession.PauseSession();

        var latestSegment = latestSession.GetLatestSegment();
        if (latestSegment == null)
            return;
        latestSegment.CloseSegment(dto.PauseReason, DateTimeOffset.UtcNow);
        
        entity.Update();
        await repository.SaveChangesAsync();
    }
    public async Task StopSessionAsync(StopSessionDto dto)
    {
        var entity = await entityRepository.GetDetailedEntityAsync(dto.EntityId);
        if (entity == null)
            return;

        entity.EndSession(dto);
        entity.Update();
        await repository.SaveChangesAsync();
    }

    public async Task ContinueSessionAsync(Guid id)
    {
        var entity = await entityRepository.GetDetailedEntityAsync(id);
        if (entity == null)
            return;

        var latestSession = entity.GetLatestSession();
        if (latestSession == null)
            return;

        latestSession.ContinueSession();
        entity.Update();
        latestSession.AddSegment(id, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync();
    }
}