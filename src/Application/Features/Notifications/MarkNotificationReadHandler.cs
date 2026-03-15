using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;

namespace Misa.Application.Features.Notifications;

public sealed record MarkNotificationReadCommand(Guid Id);

public class MarkNotificationReadHandler(INotificationRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(MarkNotificationReadCommand command, CancellationToken ct)
    {
        await repository.MarkAsReadAsync(command.Id, timeProvider.UtcNow, ct);
    }
}
