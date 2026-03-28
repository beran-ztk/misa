using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Notifications;

public sealed record MarkAllNotificationsReadCommand;

public class MarkAllNotificationsReadHandler(INotificationRepository repository)
{
    public async Task HandleAsync(MarkAllNotificationsReadCommand command, CancellationToken ct)
    {
        await repository.MarkAllAsReadAsync(DateTimeOffset.UtcNow, ct);
    }
}
