using Misa.Domain.Features.Entities.Features.Descriptions;

namespace Misa.Application.Abstractions.Persistence;

public interface IEntityRepository
{
    Task<Description?> GetDescriptionByIdAsync(Guid descriptionId, CancellationToken ct);
    void RemoveDescription(Description description);
    public Task SaveChangesAsync();
    public Task AddDescriptionAsync(Description description, CancellationToken ct);
}