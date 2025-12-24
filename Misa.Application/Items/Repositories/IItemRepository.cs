using Misa.Domain.Audit;
using Misa.Domain.Items;

namespace Misa.Application.Items.Repositories;

public interface IItemRepository
{
    Task<Item> AddAsync(Item item, CancellationToken ct);
    Task<Item?> GetTaskAsync(Guid id, CancellationToken ct = default);
    Task<List<Item>> GetAllTasksAsync(CancellationToken ct = default);
    Task<Item?> LoadAsync(Guid entityId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<Item> GetTrackedItemAsync(Guid id);
    Task<Session> GetTrackedSessionAsync(Guid id);
    Task<Session> AddSessionAsync(Session session);
    Task AddAsync(SessionSegment segment);
}