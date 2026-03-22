using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Relations;

namespace Misa.Application.Abstractions.Persistence;

public interface IItemRepository
{
    // Save changes
    Task SaveChangesAsync(CancellationToken ct = default);
    
    // Add item
    Task AddAsync(Item item, CancellationToken ct);
    
    // Inspector
    Task<Item?> TryGetItemAsync(Guid id);
    Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct);
    
    // Task extension
    Task<Item?> TryGetTaskAsync(Guid id, CancellationToken ct);
    Task<List<Item>> GetTasksAsync(CancellationToken ct);
    Task<List<Item>> GetArchivedTasksAsync(CancellationToken ct);
    Task<List<Item>> GetDeletedTasksAsync(CancellationToken ct);
    Task HardDeleteItemAsync(Guid id, CancellationToken ct);
    
    // Schedule extension
    Task<Item?> TryGetScheduleAsync(Guid id, CancellationToken ct);
    Task<List<Item>> GetSchedulesAsync(CancellationToken ct);
    
    // Journal
    Task<Item?> TryGetJournalAsync(Guid id);
    
    // Chronicle
    Task<List<Item>> GetJournalsAsync();
    Task<List<ItemActivity>> GetDeadlinesAsync();
    Task<List<Session>> GetSessionsAsync();
    Task<List<Item>> GetChangedItemsInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    
    // Zettelkasten
    Task<List<Item>> GetKnowledgeIndexAsync();
    Task<List<Item>> GetDeletedKnowledgeIndexAsync();
    Task<List<Item>> GetZettelsAsync(Guid? topicId, CancellationToken ct);
    Task<Item?> TryGetZettelAsync(Guid id, CancellationToken ct);
    Task<Item?> TryGetKnowledgeIndexItemAsync(Guid id, CancellationToken ct);
    
    // Session
    Task<Item?> TryGetItemWithSessionsAsync(Guid itemId, CancellationToken ct);
    Task<List<Session>> GetSessionsForDurationNotificationAsync(CancellationToken ct);

    // Dev
    Task DeleteAllByOwnerAsync(string ownerId, CancellationToken ct);

    // Relations
    Task AddRelationAsync(ItemRelation relation, CancellationToken ct);
    Task<List<ItemRelation>> GetRelationsForItemAsync(Guid itemId, CancellationToken ct);
    Task<ItemRelation?> TryGetRelationAsync(Guid relationId, CancellationToken ct);
    Task DeleteRelationAsync(Guid relationId, CancellationToken ct);
    Task<bool> RelationExistsAsync(Guid sourceId, Guid targetId, CancellationToken ct);
    Task<List<Item>> GetItemsForLookupAsync(CancellationToken ct);
}