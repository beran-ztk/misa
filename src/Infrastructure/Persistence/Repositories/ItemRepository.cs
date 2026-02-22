using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Exceptions;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Infrastructure.Persistence.Context;
using Item = Misa.Domain.Items.Item;

namespace Misa.Infrastructure.Persistence.Repositories;

public class ItemRepository(MisaContext context, ICurrentUser user) : IItemRepository
{
    // Save Changes
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    // Add item
    public async Task AddAsync(Item item, CancellationToken ct)
    {
        await context.Items.AddAsync(item, ct);
    }

    // Inspector
    public async Task<Item?> TryGetItemAsync(Guid id, CancellationToken ct)
    {
        return await context.Items
            .SingleOrDefaultAsync(i => i.Id == new ItemId(id) && i.OwnerId == user.Id, ct);
    }
    public async Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct)
    {
        var item = await TryGetItemAsync(id, ct);
        if (item is null) throw new DomainNotFoundException("item.not.found", id.ToString());

        switch (item.Workflow)
        {
            case Workflow.Task:
                return await TryGetTaskAsync(id, ct);
            case Workflow.Schedule:
                return await TryGetScheduleAsync(id, ct);
            default: throw new DomainConflictException("workflow.not.allowed", item.Workflow.ToString());
        }
    }
    
    // Task extension
    public async Task<Item?> TryGetTaskAsync(Guid id, CancellationToken ct)
    {
        return await context.Items
            .Include(t => t.Activity)
            .Include(t => t.TaskExtension)
            .FirstOrDefaultAsync(t 
                    => t.Id == new ItemId(id) && t.OwnerId == user.Id && t.Workflow == Workflow.Task
                , cancellationToken: ct);
    }
    
    public async Task<List<Item>> GetTasksAsync(CancellationToken ct)
    {
        return await context.Items
            .Include(t => t.Activity)
            .Include(t => t.TaskExtension)
            .Where(t => t.OwnerId == user.Id && t.Workflow == Workflow.Task)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    // Schedule extension
    public async Task<Item?> TryGetScheduleAsync(Guid id, CancellationToken ct)
    {
        return await context.Items
            .Include(t => t.ScheduleExtension)
            .FirstOrDefaultAsync(t 
                    => t.Id == new ItemId(id) && t.OwnerId == user.Id && t.Workflow == Workflow.Schedule
                , cancellationToken: ct);
    }
    public async Task<List<Item>> GetSchedulesAsync(CancellationToken ct)
    {
        return await context.Items
            .Include(s => s.ScheduleExtension)
            .Where(t => t.OwnerId == user.Id && t.Workflow == Workflow.Schedule)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    // Not yet reimplemented
    public async Task<List<Session>> GetActiveSessionsWithAutostopAsync(CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.StopAutomatically == true
                && s.State != SessionState.Ended
                && s.PlannedDuration != null)
            .Include(s => s.Segments)
            .ToListAsync(ct);
    }

    public async Task<List<Session>> GetInactiveSessionsAsync(DateTimeOffset oldestDateAllowed, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s => s.State != SessionState.Ended
            && s.Segments.Any(
                seg => seg.EndedAtUtc == null 
                       && seg.StartedAtUtc <= oldestDateAllowed)
            )
            .Include(s => s.Segments.Where(seg => seg.EndedAtUtc == null))
            .ToListAsync(ct);
    }

    public async Task<Session?> TryGetLatestCompletedSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.ItemId.Value == id
                && s.State == SessionState.Ended) 
            .Include(s => s.Segments)
            .OrderByDescending(s => s.CreatedAtUtc)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Session?> TryGetActiveSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.ItemId.Value == id
                && (s.State == SessionState.Running
                || s.State == SessionState.Paused)) 
            .Include(s => s.Segments)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Session?> TryGetRunningSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.ItemId.Value == id
                && s.State == SessionState.Running)
            .Include(s => s.Segments.Where(seg => seg.EndedAtUtc == null))
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }  
    public async Task<Session?> TryGetPausedSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.ItemId.Value == id
                && s.State == SessionState.Paused)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(Session session, CancellationToken ct)
    {
        await context.Sessions.AddAsync(session, ct);
    }
}