using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IItemRepository
{
    Task SaveChangesAsync(CancellationToken ct = default);
    Task AddAsync(Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task task, CancellationToken ct);
    Task AddAsync(Session session, CancellationToken ct);
    Task AddAsync(Scheduler scheduler, CancellationToken ct);
    Task<Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task?> TryGetTaskAsync(Guid id, CancellationToken ct);
    Task<List<Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task>> GetTasksAsync(CancellationToken ct);
    Task<Session?> TryGetLatestCompletedSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetActiveSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetRunningSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<Session?> TryGetPausedSessionByItemIdAsync(Guid id, CancellationToken ct);
    Task<List<Session>> GetActiveSessionsWithAutostopAsync(CancellationToken ct);
    Task<List<Session>> GetInactiveSessionsAsync(DateTimeOffset oldestDateAllowed, CancellationToken ct);
    
    Task<Item?> TryGetItemAsync(Guid id, CancellationToken ct);

    Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct);
    // Deadlines
    Task AddDeadlineAsync(ScheduledDeadline deadline, CancellationToken ct = default);
    Task<ScheduledDeadline?> TryGetScheduledDeadlineForItemAsync(Guid itemId, CancellationToken ct);
    Task RemoveScheduledDeadlineAsync(ScheduledDeadline obj, CancellationToken ct);
}