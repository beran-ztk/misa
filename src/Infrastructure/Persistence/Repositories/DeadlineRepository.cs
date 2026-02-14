using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Features.Common;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class DeadlineRepository(DefaultContext context) : IDeadlineRepository
{
    public async Task<Deadline?> TryGetDeadlineAsync(Guid itemId, CancellationToken ct)
    {
        return await context.Deadline
            .FirstOrDefaultAsync(d => d.ItemId == itemId, ct);
    }

    public async Task AddAsync(Deadline entity, CancellationToken ct)
    {
        await context.Deadline.AddAsync(entity, ct);
    }

    public Task Remove(Deadline entity)
    {
        context.Deadline.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await context.SaveChangesAsync(ct);
    }
}