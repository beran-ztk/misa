using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Features.Descriptions;
using Misa.Infrastructure.Persistence.Context;
using Entity = Misa.Domain.Features.Entities.Base.Entity;

namespace Misa.Infrastructure.Persistence.Repositories;

public class EntityRepository(DefaultContext db) : IEntityRepository
{
    public async Task SaveChangesAsync() => await db.SaveChangesAsync();
    public async Task<Entity> GetTrackedEntityAsync(Guid id)
        => await db.Entities.FirstAsync(e => e.Id == id);

    public async Task AddDescriptionAsync(Description description, CancellationToken ct)
    {
        await db.Descriptions.AddAsync(description, ct);
    }
}