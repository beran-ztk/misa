using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Notifications;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class NotificationRepository(MisaContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        await context.Notifications.AddAsync(notification, ct);
    }

    public async Task<List<Notification>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Notifications
            .OrderByDescending(n => n.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var rows = await context.Notifications
            .Where(n => n.Id == id)
            .ExecuteDeleteAsync(ct);
        return rows > 0;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
