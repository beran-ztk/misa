using Misa.Domain.Features.Audit;

namespace Misa.Application.Abstractions.Persistence;

public interface IChronicleRepository
{
    Task SaveChangesAsync(CancellationToken ct);
    Task AddAsync(Journal journal, CancellationToken ct);
    Task AddAsync(JournalEntry journalEntry, CancellationToken ct);
    Task<Journal> GetJournalAsync(Guid userId, CancellationToken ct);
    Task<List<JournalEntry>> GetJournalEntriesAsync(CancellationToken ct);
}