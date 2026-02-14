using Misa.Domain.Features.Common;

namespace Misa.Application.Abstractions.Persistence;

public interface IDeadlineRepository
{
    Task<Deadline?> TryGetDeadlineAsync(Guid itemId, CancellationToken ct);
    Task AddAsync(Deadline entity, CancellationToken ct);
    Task Remove(Deadline entity);
    Task SaveChangesAsync(CancellationToken ct);
}