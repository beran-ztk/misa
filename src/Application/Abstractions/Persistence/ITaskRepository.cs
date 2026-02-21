using Misa.Domain.Items;
using Misa.Domain.Items.Components.Tasks;

namespace Misa.Application.Abstractions.Persistence;

public interface ITaskRepository
{
    Task SaveChangesAsync(CancellationToken ct);
    Task AddAsync(TaskExtension taskExtension, CancellationToken ct);
    Task<Item?> TryGetTaskAsync(string userId, Guid id, CancellationToken ct);
    Task<List<Item>> GetTasksAsync(string userId, CancellationToken ct);
}