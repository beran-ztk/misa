namespace Misa.Application.Abstractions.Persistence;
using ItemTask = Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task;
public interface ITaskRepository
{
    Task SaveChangesAsync(CancellationToken ct);
    Task AddAsync(ItemTask task, CancellationToken ct);
    Task<ItemTask?> TryGetTaskAsync(Guid id, CancellationToken ct);
    Task<List<ItemTask>> GetTasksAsync(string userId, CancellationToken ct);
}