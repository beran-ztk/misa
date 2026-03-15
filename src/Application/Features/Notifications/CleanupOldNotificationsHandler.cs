using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;

namespace Misa.Application.Features.Notifications;

public sealed record CleanupOldNotificationsCommand;

public class CleanupOldNotificationsHandler(INotificationRepository repository, ITimeProvider timeProvider)
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

    public async Task<int> Handle(CleanupOldNotificationsCommand command, CancellationToken ct)
    {
        var cutoff = timeProvider.UtcNow - RetentionPeriod;
        return await repository.CleanupDismissedAsync(cutoff, ct);
    }
}
