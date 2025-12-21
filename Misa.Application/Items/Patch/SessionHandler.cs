using Misa.Application.Items.Repositories;
using Misa.Contract.Audit;
using Misa.Domain.Audit;

namespace Misa.Application.Items.Patch;

public class SessionHandler(IItemRepository repository)
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
        await repository.AddAsync(session);
        await repository.SaveChangesAsync();
    }
    public async Task PauseSessionAsync(SessionDto dto)
    {
        var hasBeenChanged = false;
        var item = await repository.GetTrackedItemAsync(dto.EntityId);
        item.PauseSession(ref hasBeenChanged);

        var session = await repository.GetTrackedSessionAsync(dto.EntityId);
        session.Pause(dto.EfficiencyId, dto.ConcentrationId, dto.Summary, DateTimeOffset.UtcNow);

        if (!hasBeenChanged)
            return;
        
        item.Entity.Update();
        await repository.SaveChangesAsync();
    }
}