using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Notifications;

public sealed record GetUnreadCountQuery;

public class GetUnreadCountHandler(NotificationRepository repository)
{
    public async Task<int> HandleAsync(GetUnreadCountQuery query, CancellationToken ct)
        => await repository.GetUnreadCountAsync(ct);
}
