using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Chronicle;
using Misa.Domain.Exceptions;

namespace Misa.Application.Features.Items.Chronicle;

public record GetJournalsCommand;

public sealed class GetJournalsHandler(IItemRepository repository)
{
    public async Task<List<ChronicleEntryDto>> HandleAsync(GetJournalsCommand command)
    {
        var chronicleEntries = new List<ChronicleEntryDto>();
        
        // Journal
        var journals = await repository.GetJournalsAsync();
        foreach (var j in journals)
        {
            var ext = j.JournalExtension
                      ?? throw new DomainConflictException("corrupted.data", "journal extension missing");

            var dto = j.ToDto();
            dto.JournalExtension = ext.ToDto();

            var entry = new ChronicleEntryDto(j.Id.Value, j.JournalExtension.OccurredAt, j.Title, j.Description,
                ChronicleEntryType.Journal);
            chronicleEntries.Add(entry);
        }

        // Deadline
        
        // Session
        
        // ExecutionLogs
        // Changes
        
        return chronicleEntries;
    }
}