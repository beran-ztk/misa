using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Notifications;

public sealed record MarkNotificationReadCommand(Guid Id);

public class MarkNotificationReadHandler(NotificationRepository repository)
{
    public async Task HandleAsync(MarkNotificationReadCommand command, CancellationToken ct)
    {
        await repository.MarkAsReadAsync(command.Id, ct);
    }
}
