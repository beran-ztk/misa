using Misa.Domain.Audit;
using Misa.Domain.Items;
using Misa.Domain.Scheduling;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IItemRepository
{
    Task<Item> AddAsync(Item item, CancellationToken ct);
    Task<Item?> GetTaskAsync(Guid id, CancellationToken ct = default);
    Task<List<Item>> GetAllTasksAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<Item?> TryGetItemAsync(Guid id);

    // Sessions
    Task<Session> AddSessionAsync(Session session);
    Task AddAsync(SessionSegment segment);

    // Deadlines
    Task AddDeadlineAsync(ScheduledDeadline deadline);
    Task<ScheduledDeadline?> TryGetScheduledDeadlineForItemAsync(Guid itemId, CancellationToken ct);
    Task RemoveScheduledDeadlineAsync(ScheduledDeadline obj, CancellationToken ct);
}