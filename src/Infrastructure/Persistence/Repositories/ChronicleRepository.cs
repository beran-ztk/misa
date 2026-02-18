using Microsoft.EntityFrameworkCore;
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

    public async Task AddAsync(JournalEntry journalEntry, CancellationToken ct)
    {
        await context.JournalEntries.AddAsync(journalEntry, ct);
    }

    public async Task<Journal> GetJournalAsync(Guid userId, CancellationToken ct)
    {
        return await context.Journals.SingleAsync(x => x.UserId == userId, ct);
    }

    public async Task<List<JournalEntry>> GetJournalEntriesAsync(CancellationToken ct)
    {
        return await context.JournalEntries.ToListAsync(ct);
    }
}