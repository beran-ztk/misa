using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Persistence;
using Misa.Infrastructure.Persistence.Context;
using ItemTask = Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task;

namespace Misa.Infrastructure.Persistence.Repositories;

public class TaskRepository(DefaultContext context) : ITaskRepository
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await context.SaveChangesAsync(ct);
    }
    
    public async Task AddAsync(ItemTask task, CancellationToken ct)
    {
        await context.Tasks.AddAsync(task, ct);
    }
    
    public async Task<ItemTask?> TryGetTaskAsync(Guid id, CancellationToken ct)
    {
        return await context.Tasks
            .Include(t => t.Item)
            .ThenInclude(i => i.Entity)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: ct);
    }
    
    public async Task<List<ItemTask>> GetTasksAsync(string userId, CancellationToken ct)
    {
        return await context.Tasks
            .Include(t => t.Item)
            .ThenInclude(i => i.Entity)
            .Where(t => t.Item.Entity.OwnerId == userId)
            .OrderByDescending(t => t.Item.Entity.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}