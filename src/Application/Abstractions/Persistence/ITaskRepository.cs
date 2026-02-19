using Misa.Domain.Items.Components.Tasks;

namespace Misa.Application.Abstractions.Persistence;

public interface ITaskRepository
{
    Task SaveChangesAsync(CancellationToken ct);
    Task AddAsync(TaskExtension taskExtension, CancellationToken ct);
    Task<TaskExtension?> TryGetTaskAsync(Guid id, CancellationToken ct);
    Task<List<TaskExtension>> GetTasksAsync(string userId, CancellationToken ct);
}