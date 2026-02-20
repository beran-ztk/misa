using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Features.Chronicle;
using Misa.Contract.Shared.Results;

namespace Misa.Application.Features.Chronicle;

public sealed record GetJournalEntriesQuery;

public class GetJournalEntriesHandler()
{
    public async Task<Result<List<JournalEntryDto>>> HandleAsync(GetJournalEntriesQuery query, CancellationToken ct)
    {
        // var journals = await repository.GetJournalEntriesAsync(ct);
        // var formattedJournals = journals.ToDto();
        // return Result<List<JournalEntryDto>>.Ok(formattedJournals);
        return Result<List<JournalEntryDto>>.Failure("not.implemented", "Has not been implemented.");
    }
}