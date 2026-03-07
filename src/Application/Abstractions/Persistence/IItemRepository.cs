using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Activities.Sessions;

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
    
    // Schedule extension
    Task<Item?> TryGetScheduleAsync(Guid id, CancellationToken ct);
    Task<List<Item>> GetSchedulesAsync(CancellationToken ct);
    
    // Chronicle
    Task<List<Item>> GetJournalsAsync();
    Task<List<ItemActivity>> GetDeadlinesAsync();
    Task<List<Session>> GetSessionsAsync();
    
    // Schola
    Task<List<Item>> GetArcsAsync();
    Task<List<Item>> GetUnitsAsync();
    
    // Session
    Task<Item?> TryGetItemWithSessionsAsync(Guid itemId, CancellationToken ct);
}