using Misa.Domain.Items;

namespace Misa.Application.Abstractions.Persistence;

public interface IItemRepository
{
    // Save changes
    Task SaveChangesAsync(CancellationToken ct = default);
    
    // Add item
    Task AddAsync(Item item, CancellationToken ct);
    
    // Inspector
    Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct);
    
    // Task extension
    Task<Item?> TryGetTaskAsync(Guid id, CancellationToken ct);
    Task<List<Item>> GetTasksAsync(CancellationToken ct);
    
    // Schedule extension
    Task<Item?> TryGetScheduleAsync(Guid id, CancellationToken ct);
    Task<List<Item>> GetSchedulesAsync(CancellationToken ct);
    
    // Session
    Task<Item?> TryGetItemWithSessionsAsync(Guid itemId, CancellationToken ct);
}