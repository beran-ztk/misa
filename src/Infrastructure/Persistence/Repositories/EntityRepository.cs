using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Features.Descriptions;
using Misa.Infrastructure.Persistence.Context;
using Entity = Misa.Domain.Features.Entities.Base.Entity;

namespace Misa.Infrastructure.Persistence.Repositories;

public class EntityRepository(DefaultContext context) : IEntityRepository
{
    public async Task SaveChangesAsync() => await context.SaveChangesAsync();
    public Task<Description?> GetDescriptionByIdAsync(Guid descriptionId, CancellationToken ct)
        => context.Descriptions.FirstOrDefaultAsync(d => d.Id == descriptionId, ct);

    public void RemoveDescription(Description description)
        => context.Descriptions.Remove(description);
    public async Task AddDescriptionAsync(Description description, CancellationToken ct)
    {
        await context.Descriptions.AddAsync(description, ct);
    }
}