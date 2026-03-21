using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Exceptions;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Relations;
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
    public async Task<Item?> TryGetItemAsync(Guid id)
    {
        return await context.Items
            .SingleOrDefaultAsync(i => i.Id == new ItemId(id) && i.OwnerId == user.Id);
    }
    public async Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct)
    {
        var item = await TryGetItemAsync(id);
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
            .Include(t => t.Changes)
            .FirstOrDefaultAsync(t
                    => t.Id == new ItemId(id) && t.OwnerId == user.Id && t.Workflow == Workflow.Task
                , cancellationToken: ct);
    }
    
    public async Task<List<Item>> GetTasksAsync(CancellationToken ct)
    {
        return await context.Items
            .Include(t => t.Activity)
            .Include(t => t.TaskExtension)
            .Where(t => t.OwnerId == user.Id && t.Workflow == Workflow.Task && !t.IsDeleted && !t.IsArchived)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<Item>> GetArchivedTasksAsync(CancellationToken ct)
    {
        return await context.Items
            .Include(t => t.Activity)
            .Include(t => t.TaskExtension)
            .Where(t => t.OwnerId == user.Id && t.Workflow == Workflow.Task && t.IsArchived && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<Item>> GetDeletedTasksAsync(CancellationToken ct)
    {
        return await context.Items
            .Include(t => t.Activity)
            .Include(t => t.TaskExtension)
            .Where(t => t.OwnerId == user.Id && t.Workflow == Workflow.Task && t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task HardDeleteItemAsync(Guid id, CancellationToken ct)
    {
        await context.Items
            .Where(i => i.Id == new ItemId(id) && i.OwnerId == user.Id)
            .ExecuteDeleteAsync(ct);
    }

    // Schedule extension
    public async Task<Item?> TryGetScheduleAsync(Guid id, CancellationToken ct)
    {
        return await context.Items
            .Include(t => t.ScheduleExtension)
            .Include(t => t.Changes)
            .FirstOrDefaultAsync(t
                    => t.Id == new ItemId(id) && t.OwnerId == user.Id && t.Workflow == Workflow.Schedule
                , cancellationToken: ct);
    }
    public async Task<List<Item>> GetSchedulesAsync(CancellationToken ct)
    {
        return await context.Items
            .Include(s => s.ScheduleExtension)
            .Where(t => t.OwnerId == user.Id && t.Workflow == Workflow.Schedule && t.IsDeleted == false && t.IsArchived == false)
            .OrderByDescending(t => t.JournalExtension!.OccurredAt) 
            .ThenByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Item?> TryGetJournalAsync(Guid id)
    {
        return await context.Items
            .Include(z => z.JournalExtension)
            .FirstOrDefaultAsync(
                z => z.Id == new ItemId(id) && z.OwnerId == user.Id && z.Workflow == Workflow.Journal);
    }

    public async Task<List<Item>> GetJournalsAsync()
    {
        return await context.Items
            .Include(s => s.JournalExtension)
            .Where(t => t.OwnerId == user.Id && t.Workflow == Workflow.Journal && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<ItemActivity>> GetDeadlinesAsync()
    {
        return await context.ItemActivities
            .Include(a => a.Item)
            .Where(a => a.DueAt != null)
            .OrderByDescending(a => a.DueAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Session>> GetSessionsAsync()
    {
        return await context.Sessions
            .Include(s => s.Segments)
            .Include(s => s.Item)
            .Where(s => s.State == SessionState.Ended)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Item>> GetChangedItemsInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        return await context.Items
            .Where(i => i.OwnerId == user.Id
                && i.ModifiedAt != null
                && i.ModifiedAt >= from
                && i.ModifiedAt <= to)
            .OrderByDescending(i => i.ModifiedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<Item>> GetArcsAsync()
    {
        return await context.Items
            .Include(s => s.Activity)
            .Include(s => s.Arc)
            .Where(t => t.OwnerId == user.Id && t.Workflow == Workflow.Arc)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Item>> GetUnitsAsync()
    {
        return await context.Items
            .Include(s => s.Activity)
            .Include(s => s.Unit)
            .Where(t => t.OwnerId == user.Id && t.Workflow == Workflow.Unit)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Item>> GetKnowledgeIndexAsync()
    {
        return await context.Items
            .Include(s => s.KnowledgeIndex)
            .Where(t => t.OwnerId == user.Id 
                        && t.Workflow == Workflow.Topic && t.Workflow == Workflow.Zettel)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Item>> GetZettelsAsync(Guid? topicId, CancellationToken ct)
    {
        var query = context.Items
            .Include(z => z.ZettelExtension)
            .Include(z => z.KnowledgeIndex)
            .Where(z => z.OwnerId == user.Id && z.Workflow == Workflow.Zettel);

        if (topicId.HasValue)
            query = query.Where(z => z.KnowledgeIndex!.ParentId == new ItemId(topicId.Value));

        return await query
            .OrderByDescending(z => z.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Item?> TryGetZettelAsync(Guid id, CancellationToken ct)
    {
        return await context.Items
            .Include(z => z.ZettelExtension)
            .FirstOrDefaultAsync(
                z => z.Id == new ItemId(id) && z.OwnerId == user.Id && z.Workflow == Workflow.Zettel,
                ct);
    }

    // Session
    public async Task<List<Session>> GetSessionsForDurationNotificationAsync(CancellationToken ct)
    {
        return await context.Sessions
            .Include(s => s.Segments)
            .Include(s => s.Item)
            .Where(s => s.State == SessionState.Running
                     && s.PlannedDuration != null
                     && !s.PlannedDurationNotificationSent)
            .ToListAsync(ct);
    }

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

    // Dev
    public async Task DeleteAllByOwnerAsync(string ownerId, CancellationToken ct)
    {
        await context.Items
            .Where(i => i.OwnerId == ownerId)
            .ExecuteDeleteAsync(ct);
    }

    // Relations
    public async Task AddRelationAsync(ItemRelation relation, CancellationToken ct)
    {
        await context.ItemRelations.AddAsync(relation, ct);
    }

    public async Task<List<ItemRelation>> GetRelationsForItemAsync(Guid itemId, CancellationToken ct)
    {
        var id = new ItemId(itemId);
        return await context.ItemRelations
            .Include(r => r.SourceItem)
            .Include(r => r.TargetItem)
            .Where(r => r.SourceItemId == id || r.TargetItemId == id)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<ItemRelation?> TryGetRelationAsync(Guid relationId, CancellationToken ct)
    {
        return await context.ItemRelations
            .FirstOrDefaultAsync(r => r.Id == relationId, ct);
    }

    public async Task DeleteRelationAsync(Guid relationId, CancellationToken ct)
    {
        var relation = await context.ItemRelations
            .FirstOrDefaultAsync(r => r.Id == relationId, ct);
        if (relation is not null)
            context.ItemRelations.Remove(relation);
    }

    public async Task<List<Item>> GetItemsForLookupAsync(CancellationToken ct)
    {
        return await context.Items
            .Where(i => i.OwnerId == user.Id && !i.IsDeleted && !i.IsArchived)
            .OrderBy(i => i.Title)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}