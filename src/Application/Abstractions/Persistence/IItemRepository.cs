using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Application.Abstractions.Persistence;

public interface IItemRepository
{
    Task SaveChangesAsync(CancellationToken ct = default);
    Task AddAsync(Item item, CancellationToken ct);
    
    Task AddAsync(Session session, CancellationToken ct);
    Task AddAsync(ScheduleExtension scheduleExtension, CancellationToken ct);
    Task<Session?> TryGetLatestCompletedSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetActiveSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetRunningSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetPausedSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<List<Session>> GetActiveSessionsWithAutostopAsync(CancellationToken ct);
    Task<List<Session>> GetInactiveSessionsAsync(DateTimeOffset oldestDateAllowed, CancellationToken ct);

    Task<Item?> TryGetItemAsync(Guid id, CancellationToken ct);
    
    Task<List<Item>> GetSchedulingRulesAsync(CancellationToken ct);
}