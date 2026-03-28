using Microsoft.EntityFrameworkCore;
using Misa.Core.Abstractions.Persistence;
using Misa.Domain.Notifications;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class NotificationRepository(MisaContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        await context.Notifications.AddAsync(notification, ct);
    }

    public async Task<List<Notification>> GetPageAsync(int limit, DateTimeOffset? before, bool onlyUnread, CancellationToken ct = default)
    {
        var query = context.Notifications
            .Where(n => n.DismissedAtUtc == null);

        if (onlyUnread)
            query = query.Where(n => n.ReadAtUtc == null);

        if (before.HasValue)
            query = query.Where(n => n.CreatedAtUtc < before.Value);

        return await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct = default)
    {
        return await context.Notifications
            .Where(n => n.DismissedAtUtc == null && n.ReadAtUtc == null)
            .CountAsync(ct);
    }

    public async Task DismissAsync(Guid id, DateTimeOffset dismissedAt, CancellationToken ct = default)
    {
        await context.Notifications
            .Where(n => n.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.DismissedAtUtc, dismissedAt), ct);
    }

    public async Task MarkAsReadAsync(Guid id, DateTimeOffset readAt, CancellationToken ct = default)
    {
        await context.Notifications
            .Where(n => n.Id == id && n.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAtUtc, readAt), ct);
    }

    public async Task MarkAllAsReadAsync(DateTimeOffset readAt, CancellationToken ct = default)
    {
        await context.Notifications
            .Where(n => n.DismissedAtUtc == null && n.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAtUtc, readAt), ct);
    }

    public async Task<int> CleanupDismissedAsync(DateTimeOffset dismissedBefore, CancellationToken ct = default)
    {
        return await context.Notifications
            .Where(n => n.DismissedAtUtc != null && n.DismissedAtUtc < dismissedBefore && n.DeletedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAtUtc, dismissedBefore), ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
