using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Notifications;

public sealed record MarkAllNotificationsReadCommand;

public class MarkAllNotificationsReadHandler(NotificationRepository repository)
{
    public async Task HandleAsync(MarkAllNotificationsReadCommand command, CancellationToken ct)
    {
        await repository.MarkAllAsReadAsync(ct);
    }
}
