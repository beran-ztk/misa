using Misa.Application.Abstractions.Persistence;

namespace Misa.Application.Features.Notifications;

public sealed record GetUnreadCountQuery;

public class GetUnreadCountHandler(INotificationRepository repository)
{
    public async Task<int> HandleAsync(GetUnreadCountQuery query, CancellationToken ct)
        => await repository.GetUnreadCountAsync(ct);
}
