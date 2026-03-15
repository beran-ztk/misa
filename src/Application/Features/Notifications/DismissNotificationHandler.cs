using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;

namespace Misa.Application.Features.Notifications;

public sealed record DismissNotificationCommand(Guid Id);

public class DismissNotificationHandler(INotificationRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(DismissNotificationCommand command, CancellationToken ct)
    {
        await repository.DismissAsync(command.Id, timeProvider.UtcNow, ct);
        await repository.SaveChangesAsync(ct);
    }
}
