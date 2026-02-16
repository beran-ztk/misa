using Misa.Domain.Features.Audit;

namespace Misa.Application.Abstractions.Persistence;

public interface IChronicleRepository
{
    Task SaveChangesAsync(CancellationToken ct);
    Task AddAsync(Journal journal, CancellationToken ct);
}