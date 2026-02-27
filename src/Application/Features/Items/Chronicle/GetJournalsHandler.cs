using Misa.Application.Abstractions.Persistence;
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
            if (j.JournalExtension is null) 
                throw new DomainConflictException("corrupted.data", "journal extension missing");

            var entry = new ChronicleEntryDto(j.Id.Value, j.JournalExtension.OccurredAt, j.Title, j.Description,
                ChronicleEntryType.Journal);
            chronicleEntries.Add(entry);
        }

        // Deadline
        var deadlines = await repository.GetDeadlinesAsync();
        foreach (var d in deadlines)
        {
            var dueAt = d.DueAt!.Value;
            var entry = new ChronicleEntryDto(d.Id.Value, dueAt, $"Deadline for {d.Item.Title}", null,
                ChronicleEntryType.Deadline);
            chronicleEntries.Add(entry);
        }
        
        // Session
        var sessions = await repository.GetSessionsAsync();
        foreach (var s in sessions)
        {
            var entry = new ChronicleEntryDto(s.ItemId.Value, s.CreatedAtUtc, $"Session for {s.Item.Title}", null,
                ChronicleEntryType.Session);
            chronicleEntries.Add(entry);
        }
        // ExecutionLogs
        // Changes
        
        return chronicleEntries;
    }
}