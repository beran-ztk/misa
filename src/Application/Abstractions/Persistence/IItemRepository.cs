using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Application.Abstractions.Persistence;

public interface IItemRepository
{
    Task SaveChangesAsync(CancellationToken ct = default);
    Task AddAsync(Session session, CancellationToken ct);
    Task AddAsync(ScheduleExtension scheduleExtension, CancellationToken ct);
    Task<Session?> TryGetLatestCompletedSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetActiveSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetRunningSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetPausedSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<List<Session>> GetActiveSessionsWithAutostopAsync(CancellationToken ct);
    Task<List<Session>> GetInactiveSessionsAsync(DateTimeOffset oldestDateAllowed, CancellationToken ct);
    
    Task<Item?> TryGetItemAsync(Guid id, CancellationToken ct);

    Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct);
    
    Task<List<Item>> GetSchedulingRulesAsync(CancellationToken ct);
}