using Misa.Domain.Audit;
using Misa.Domain.Entities;
using Misa.Domain.Items;
using Misa.Domain.Scheduling;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IItemRepository
{
    Task<Item> AddAsync(Item item, CancellationToken ct);
    Task AddAsync(Session session, CancellationToken ct);
    Task<Session?> TryGetLatestCompletedSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetActiveSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetRunningSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetPausedSessionByItemIdAsync(Guid id, CancellationToken ct);
    
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<Item?> TryGetItemAsync(Guid id, CancellationToken ct);

    Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct);
    // Tasks
    Task<List<Item>> TryGetTasksAsync(CancellationToken ct);
    // Sessions

    // Deadlines
    Task AddDeadlineAsync(ScheduledDeadline deadline, CancellationToken ct = default);
    Task<ScheduledDeadline?> TryGetScheduledDeadlineForItemAsync(Guid itemId, CancellationToken ct);
    Task RemoveScheduledDeadlineAsync(ScheduledDeadline obj, CancellationToken ct);
}