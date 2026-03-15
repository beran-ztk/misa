using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Notifications;

namespace Misa.Application.Features.Notifications;

public sealed record GetNotificationsQuery;

public class GetNotificationsHandler(INotificationRepository repository)
{
    public async Task<List<NotificationEntryDto>> HandleAsync(
        GetNotificationsQuery query,
        CancellationToken ct)
    {
        var notifications = await repository.GetAllAsync(ct);

        return notifications
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new NotificationEntryDto(
                n.Id,
                n.Title,
                n.Message,
                n.SourceId,
                n.CreatedAtUtc))
            .ToList();
    }
}
