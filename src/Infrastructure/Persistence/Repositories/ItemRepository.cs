using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Exceptions;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Infrastructure.Persistence.Context;
using Wolverine;
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
            .ThenInclude(a => a.Sessions)
            .ThenInclude(s => s.Segments)
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

    // Session
    public async Task<Item?> TryGetItemWithSessionsAsync(Guid itemId, CancellationToken ct)
    {
        return await context.Items
            .Include(t => t.Activity)
            .ThenInclude(a => a.Sessions)
            .ThenInclude(s => s.Segments)
            .FirstOrDefaultAsync(
                i => i.Id == new ItemId(itemId) && i.OwnerId == user.Id && i.Activity != null,
                ct
            );
    }
}