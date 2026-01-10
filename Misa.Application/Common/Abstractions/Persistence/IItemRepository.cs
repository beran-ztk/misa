using Misa.Domain.Audit;
using Misa.Domain.Entities;
using Misa.Domain.Items;
using Misa.Domain.Scheduling;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IItemRepository
{
    Task<Item> AddAsync(Item item, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<Item?> TryGetItemAsync(Guid id, CancellationToken ct);

    Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct);
    // Tasks
    Task<List<Item>> TryGetTasksAsync(CancellationToken ct);
    // Sessions
    Task<Session> AddSessionAsync(Session session);
    Task AddAsync(SessionSegment segment);

    // Deadlines
    Task AddDeadlineAsync(ScheduledDeadline deadline);
    Task<ScheduledDeadline?> TryGetScheduledDeadlineForItemAsync(Guid itemId, CancellationToken ct);
    Task RemoveScheduledDeadlineAsync(ScheduledDeadline obj, CancellationToken ct);
}