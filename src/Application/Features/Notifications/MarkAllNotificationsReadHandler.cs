using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;

namespace Misa.Application.Features.Notifications;

public sealed record MarkAllNotificationsReadCommand;

public class MarkAllNotificationsReadHandler(INotificationRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(MarkAllNotificationsReadCommand command, CancellationToken ct)
    {
        await repository.MarkAllAsReadAsync(timeProvider.UtcNow, ct);
    }
}
