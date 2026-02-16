using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Features.Audit;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public sealed class ChronicleRepository(DefaultContext context) : IChronicleRepository
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await context.SaveChangesAsync(ct);
    }
    
    public async Task AddAsync(Journal journal, CancellationToken ct)
    {
        await context.Journals.AddAsync(journal, ct);
    }
}