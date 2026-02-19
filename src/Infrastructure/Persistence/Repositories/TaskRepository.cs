using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Items.Components.Tasks;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class TaskRepository(MisaContext context) : ITaskRepository
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await context.SaveChangesAsync(ct);
    }
    
    public async Task AddAsync(TaskExtension taskExtension, CancellationToken ct)
    {
        await context.Tasks.AddAsync(taskExtension, ct);
    }
    
    public async Task<TaskExtension?> TryGetTaskAsync(Guid id, CancellationToken ct)
    {
        return await context.Tasks
            .Include(t => t.Item)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: ct);
    }
    
    public async Task<List<TaskExtension>> GetTasksAsync(string userId, CancellationToken ct)
    {
        return await context.Tasks
            .Include(t => t.Item)
            .Where(t => t.Item.OwnerId == userId)
            .OrderByDescending(t => t.Item.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}