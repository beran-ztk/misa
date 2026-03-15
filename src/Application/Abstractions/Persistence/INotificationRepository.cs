using Misa.Domain.Notifications;

namespace Misa.Application.Abstractions.Persistence;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<List<Notification>> GetPageAsync(int limit, DateTimeOffset? before, CancellationToken ct = default);
    Task DismissAsync(Guid id, DateTimeOffset dismissedAt, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
