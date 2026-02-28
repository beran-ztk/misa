using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Contract.Common.Converters;
using Misa.Contract.Items.Components.Chronicle;
using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Activities.Sessions;

namespace Misa.Application.Features.Items.Chronicle;

public record GetJournalsCommand;

public sealed class GetJournalsHandler(IItemRepository repository, ITimeProvider timeProvider)
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

            var entry = new ChronicleEntryDto(
                TargetItemId: j.Id.Value,
                At: j.JournalExtension.OccurredAt,
                Title: j.Title,
                Type: ChronicleEntryType.Journal,
                MetaState: null,
                MetaText: null,
                Description: j.Description
            );

            chronicleEntries.Add(entry);
        }

        // Deadline
        var deadlines = await repository.GetDeadlinesAsync();
        foreach (var d in deadlines)
        {
            var now = timeProvider.UtcNow;
            var dueAt = d.DueAt!.Value;
            
            var state = d.DueAt < now 
                ? ChronicleMetaState.Overdue 
                : ChronicleMetaState.Due;  
            
            var entry = new ChronicleEntryDto(
                TargetItemId: d.Id.Value,
                At: dueAt,
                Title: d.Item.Title,
                Type: ChronicleEntryType.Deadline,
                MetaState: state,
                MetaText: $"Due {dueAt:dd.MM.yyyy HH:mm}",
                Description: null
            );

            chronicleEntries.Add(entry);
        }
        
        // Session
        var sessions = await repository.GetSessionsAsync();
        foreach (var s in sessions)
        {
            var now = timeProvider.UtcNow;
            ChronicleMetaState? state = s.State != SessionState.Ended 
                ? ChronicleMetaState.Active 
                : null;
            
            List<StartToEndTimestamp?> tss = [];
            foreach (var seg in s.Segments)
            {
                tss.Add(new StartToEndTimestamp(seg.StartedAtUtc, seg.EndedAtUtc));
            }
            
            var elapsedTime = TimeSpanCalculator.ElapsedTime(tss);
            var metaText = TimeSpanCalculator.FormatDuration(elapsedTime);
            
            var entry = new ChronicleEntryDto(
                TargetItemId: s.ItemId.Value,
                At: s.CreatedAtUtc,
                Title: s.Item.Title,
                Type: ChronicleEntryType.Session,
                MetaState: state,
                MetaText: $"Elapsed Time: {metaText}",
                Description: $"Summary: {s.Summary}"
            );

            chronicleEntries.Add(entry);
        }
        // ExecutionLogs
        // Changes
        
        return chronicleEntries;
    }
}