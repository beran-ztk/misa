using Misa.Domain.Items;

namespace Misa.Application.Items.Repositories;

public interface IItemRepository
{
    Task<Item> AddAsync(Item item, CancellationToken ct);
    Task<List<Item>> GetAllTasksAsync(CancellationToken ct = default);
    Task<Item?> LoadAsync(Guid entityId, CancellationToken ct = default);
}