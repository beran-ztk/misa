using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Notifications;

namespace Misa.Application.Features.Notifications;

public sealed record GetNotificationsQuery(int Limit = 25, DateTimeOffset? Before = null);

public class GetNotificationsHandler(INotificationRepository repository)
{
    public async Task<List<NotificationEntryDto>> HandleAsync(
        GetNotificationsQuery query,
        CancellationToken ct)
    {
        var notifications = await repository.GetPageAsync(query.Limit, query.Before, ct);

        return notifications
            .Select(n => new NotificationEntryDto(
                n.Id,
                n.Title,
                n.Message,
                n.SourceId,
                n.CreatedAtUtc))
            .ToList();
    }
}
