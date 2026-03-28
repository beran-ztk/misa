using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Common.Abstractions.Time;

namespace Misa.Core.Features.Notifications;

public sealed record MarkAllNotificationsReadCommand;

public class MarkAllNotificationsReadHandler(INotificationRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(MarkAllNotificationsReadCommand command, CancellationToken ct)
    {
        await repository.MarkAllAsReadAsync(timeProvider.UtcNow, ct);
    }
}
