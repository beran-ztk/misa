using Microsoft.EntityFrameworkCore;
using Misa.Domain.Notifications;
using Misa.Infrastructure.Persistence;

namespace Misa.Core.Persistence;

public class NotificationRepository(Context context)
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

    public async Task DismissAsync(Guid id, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await context.Notifications
            .Where(n => n.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.DismissedAtUtc, now), ct);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await context.Notifications
            .Where(n => n.Id == id && n.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAtUtc, now), ct);
    }

    public async Task MarkAllAsReadAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await context.Notifications
            .Where(n => n.DismissedAtUtc == null && n.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAtUtc, now), ct);
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
