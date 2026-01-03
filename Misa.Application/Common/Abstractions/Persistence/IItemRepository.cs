using Misa.Domain.Audit;
using Misa.Domain.Items;
using Misa.Domain.Scheduling;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IItemRepository
{
    Task<Item> AddAsync(Item item, CancellationToken ct);
    Task<Item?> GetTaskAsync(Guid id, CancellationToken ct = default);
    Task<List<Item>> GetAllTasksAsync(CancellationToken ct = default);
    Task<Item?> LoadAsync(Guid entityId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<Item?> GetTrackedItemAsync(Guid id);

    // Sessions
    Task<Session> GetTrackedSessionAsync(Guid id);
    Task<Session> AddSessionAsync(Session session);
    Task AddAsync(SessionSegment segment);

    // Deadlines (Item-owned)
    Task<ScheduledDeadline?> GetSingleTrackedDeadlineAsync(Guid itemId, CancellationToken ct = default);
    Task AddDeadlineAsync(ScheduledDeadline deadline);
    Task RemoveDeadlineAsync(Guid itemId, CancellationToken ct = default);
}