using Misa.Core.Abstractions.Persistence;
using Misa.Core.Abstractions.Time;

namespace Misa.Core.Features.Notifications;

public sealed record MarkNotificationReadCommand(Guid Id);

public class MarkNotificationReadHandler(INotificationRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(MarkNotificationReadCommand command, CancellationToken ct)
    {
        await repository.MarkAsReadAsync(command.Id, timeProvider.UtcNow, ct);
    }
}
