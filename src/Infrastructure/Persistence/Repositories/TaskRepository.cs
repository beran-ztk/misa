using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Items;
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
    
    public async Task<Item?> TryGetTaskAsync(Guid id, CancellationToken ct)
    {
        return await context.Items
            .Include(t => t.Activity)
            .Include(t => t.TaskExtension)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == new ItemId(id), cancellationToken: ct);
    }
    
    public async Task<List<Item>> GetTasksAsync(string userId, CancellationToken ct)
    {
        return await context.Items
            .Include(t => t.Activity)
            .Include(t => t.TaskExtension)
            .Where(t => t.OwnerId == userId && t.Workflow == Workflow.Task)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}