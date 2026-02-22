using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities.Sessions;

namespace Misa.Application.Abstractions.Persistence;

public interface IItemRepository
{
    // Save changes
    Task SaveChangesAsync(CancellationToken ct = default);
    
    // Add item
    Task AddAsync(Item item, CancellationToken ct);
    
    // Inspector
    Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct);
    
    // Task extension
    Task<Item?> TryGetTaskAsync(Guid id, CancellationToken ct);
    Task<List<Item>> GetTasksAsync(CancellationToken ct);
    
    // Schedule extension
    Task<Item?> TryGetScheduleAsync(Guid id, CancellationToken ct);
    Task<List<Item>> GetSchedulesAsync(CancellationToken ct);
    
    // Not yet reimplemented
    Task AddAsync(Session session, CancellationToken ct);
    Task<Session?> TryGetLatestCompletedSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetActiveSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetRunningSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetPausedSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<List<Session>> GetActiveSessionsWithAutostopAsync(CancellationToken ct);
    Task<List<Session>> GetInactiveSessionsAsync(DateTimeOffset oldestDateAllowed, CancellationToken ct);

    Task<Item?> TryGetItemAsync(Guid id, CancellationToken ct);
}