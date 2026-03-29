using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Items.Chronicle;

public record GetChronicleEntriesCommand(DateTimeOffset From, DateTimeOffset To);

public enum ChronicleMetaState
{
    Active,
    Due,
    Overdue,
    Completed
}
public enum ChronicleEntryType
{
    Journal,
    Session,
    Deadline,
    SchedulerEvent,
    AuditChange
}
public sealed class GetChronicleEntriesHandler(ItemRepository repository)
{
    public async Task HandleAsync(GetChronicleEntriesCommand entriesCommand)
    {
        // var chronicleEntries = new List<ChronicleEntryDto>();
        //
        // // Journal
        // var journals = await repository.GetJournalsAsync();
        // foreach (var j in journals)
        // {
        //     if (j.JournalExtension is null)
        //         throw new DomainConflictException("corrupted.data", "journal extension missing");
        //
        //     var at = j.JournalExtension.OccurredAt;
        //     if (at < query.From || at > query.To) continue;
        //
        //     chronicleEntries.Add(new ChronicleEntryDto(
        //         TargetItemId: j.Id.Value,
        //         At: at,
        //         Title: j.Title,
        //         Type: ChronicleEntryType.Journal,
        //         MetaState: null,
        //         MetaText: null,
        //         Description: j.Description
        //     ));
        // }
        //
        // // Deadline
        // var deadlines = await repository.GetDeadlinesAsync();
        // foreach (var d in deadlines)
        // {
        //     var dueAt = d.DueAt!.Value;
        //     if (dueAt < query.From || dueAt > query.To) continue;
        //
        //     var now = timeProvider.UtcNow;
        //     var state = dueAt < now ? ChronicleMetaState.Overdue : ChronicleMetaState.Due;
        //
        //     chronicleEntries.Add(new ChronicleEntryDto(
        //         TargetItemId: d.Id.Value,
        //         At: dueAt,
        //         Title: d.Item.Title,
        //         Type: ChronicleEntryType.Deadline,
        //         MetaState: state,
        //         MetaText: $"Due {dueAt:dd.MM.yyyy HH:mm}",
        //         Description: null
        //     ));
        // }
        //
        // // Session
        // var sessions = await repository.GetSessionsAsync();
        // foreach (var s in sessions)
        // {
        //     var at = s.CreatedAtUtc;
        //     if (at < query.From || at > query.To) continue;
        //
        //     var now = timeProvider.UtcNow;
        //     ChronicleMetaState? state = s.State != SessionState.Ended ? ChronicleMetaState.Active : null;
        //
        //     List<StartToEndTimestamp?> tss = [];
        //     foreach (var seg in s.Segments)
        //         tss.Add(new StartToEndTimestamp(seg.StartedAtUtc, seg.EndedAtUtc));
        //
        //     var elapsed = TimeSpanCalculator.ElapsedTime(tss);
        //     var metaText = TimeSpanCalculator.FormatDuration(elapsed);
        //
        //     chronicleEntries.Add(new ChronicleEntryDto(
        //         TargetItemId: s.ItemId.Value,
        //         At: at,
        //         Title: s.Item.Title,
        //         Type: ChronicleEntryType.Session,
        //         MetaState: state,
        //         MetaText: $"Elapsed: {metaText}",
        //         Description: string.IsNullOrWhiteSpace(s.Summary) ? null : s.Summary
        //     ));
        // }
        //
        // // AuditChange
        // var changedItems = await repository.GetChangedItemsInRangeAsync(query.From, query.To);
        // foreach (var item in changedItems)
        // {
        //     var at = item.ModifiedAt!.Value;
        //     chronicleEntries.Add(new ChronicleEntryDto(
        //         TargetItemId: item.Id.Value,
        //         At: at,
        //         Title: item.Title,
        //         Type: ChronicleEntryType.AuditChange,
        //         MetaState: null,
        //         MetaText: "Modified",
        //         Description: null
        //     ));
        // }
    }
}
