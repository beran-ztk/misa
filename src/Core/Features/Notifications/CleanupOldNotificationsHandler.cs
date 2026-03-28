using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Common.Abstractions.Time;

namespace Misa.Core.Features.Notifications;

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
