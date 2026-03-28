using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Notifications;

public sealed record DismissNotificationCommand(Guid Id);

public class DismissNotificationHandler(NotificationRepository repository)
{
    public async Task HandleAsync(DismissNotificationCommand command, CancellationToken ct)
    {
        await repository.DismissAsync(command.Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
