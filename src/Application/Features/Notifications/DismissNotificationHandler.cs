using Misa.Application.Abstractions.Persistence;

namespace Misa.Application.Features.Notifications;

public sealed record DismissNotificationCommand(Guid Id);

public class DismissNotificationHandler(INotificationRepository repository)
{
    public async Task HandleAsync(DismissNotificationCommand command, CancellationToken ct)
    {
        await repository.DeleteAsync(command.Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
