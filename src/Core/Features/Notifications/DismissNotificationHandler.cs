using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Common.Abstractions.Time;

namespace Misa.Core.Features.Notifications;

public sealed record DismissNotificationCommand(Guid Id);

public class DismissNotificationHandler(INotificationRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(DismissNotificationCommand command, CancellationToken ct)
    {
        await repository.DismissAsync(command.Id, timeProvider.UtcNow, ct);
        await repository.SaveChangesAsync(ct);
    }
}
