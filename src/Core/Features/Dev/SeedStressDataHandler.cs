using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Dev;

public record SeedStressDataCommand;

public sealed class SeedStressDataHandler(ItemRepository repository)
{
    // private static readonly TaskCategory[]         _categories    = Enum.GetValues<TaskCategory>();
    // private static readonly ActivityPriority[]     _priorities    = Enum.GetValues<ActivityPriority>();
    // private static readonly SessionEfficiencyType[]    _efficiencies  = Enum.GetValues<SessionEfficiencyType>();
    // private static readonly SessionConcentrationType[] _concentrations = Enum.GetValues<SessionConcentrationType>();
    //
    // public async Task HandleAsync(SeedStressDataCommand command, CancellationToken ct)
    // {
    //     var now   = timeProvider.UtcNow;
    //     var owner = currentUser.Id;
    //     var tz    = currentUser.Timezone;
    //
    //     await SeedTasksAsync(now, owner, ct);
    //     await SeedJournalsAsync(now, owner, ct);
    //     await SeedSchedulesAsync(now, owner, tz, ct);
    // }
    //
    // // ── Tasks ──────────────────────────────────────────────────────────────
    //
    // private async Task SeedTasksAsync(DateTimeOffset now, string owner, CancellationToken ct)
    // {
    //     // Open — 120 items
    //     for (var i = 1; i <= 120; i++)
    //     {
    //         var category = _categories[i % _categories.Length];
    //         var priority = _priorities[i % _priorities.Length];
    //         DateTimeOffset? dueAt = (i % 8) switch
    //         {
    //             0 => now.AddHours(-6),       // overdue
    //             1 => now.AddHours(3),        // due imminently
    //             2 => now.AddDays(1),         // due tomorrow
    //             3 => now.AddDays(4),         // due in a few days
    //             _ => null
    //         };
    //
    //         var item = Item.CreateTask(
    //             new ItemId(idGenerator.New()), owner,
    //             $"Seed Task {i:D3}",
    //             $"Stress seed open task number {i}. Category: {category}, Priority: {priority}.",
    //             category,
    //             now.AddDays(-(i % 30)).AddHours(-(i % 24)),
    //             priority,
    //             dueAt: dueAt);
    //
    //         if (i % 4 == 0)
    //             AddCompletedSession(item,
    //                 now.AddDays(-(i % 10)).AddHours(-(i % 12)),
    //                 TimeSpan.FromMinutes(20 + i % 80),
    //                 $"Work session for task {i:D3}",
    //                 _efficiencies[i % _efficiencies.Length],
    //                 _concentrations[i % _concentrations.Length],
    //                 $"Made progress on task {i:D3}.");
    //
    //         await repository.AddAsync(item, ct);
    //     }
    //
    //     // Done — 60 items
    //     for (var i = 1; i <= 60; i++)
    //     {
    //         var category = _categories[i % _categories.Length];
    //         var priority = _priorities[i % _priorities.Length];
    //
    //         var item = Item.CreateTask(
    //             new ItemId(idGenerator.New()), owner,
    //             $"Seed Task Done {i:D3}",
    //             $"Stress seed completed task number {i}.",
    //             category,
    //             now.AddDays(-(30 + i % 30)).AddHours(-(i % 24)),
    //             priority,
    //             dueAt: null);
    //
    //         if (i % 2 == 0)
    //             AddCompletedSession(item,
    //                 now.AddDays(-(i % 20)).AddHours(-(i % 8)),
    //                 TimeSpan.FromMinutes(30 + i % 90),
    //                 $"Completion session for done task {i:D3}",
    //                 _efficiencies[i % _efficiencies.Length],
    //                 _concentrations[i % _concentrations.Length],
    //                 $"Wrapped up task {i:D3}.");
    //
    //         item.Activity.ChangeState(ActivityState.Done, now);
    //         await repository.AddAsync(item, ct);
    //     }
    //
    //     // Cancelled — 30 items
    //     for (var i = 1; i <= 30; i++)
    //     {
    //         var category = _categories[i % _categories.Length];
    //         var priority = _priorities[i % _priorities.Length];
    //
    //         var item = Item.CreateTask(
    //             new ItemId(idGenerator.New()), owner,
    //             $"Seed Task Cancelled {i:D3}",
    //             $"Stress seed cancelled task number {i}.",
    //             category,
    //             now.AddDays(-(60 + i % 30)).AddHours(-(i % 24)),
    //             priority,
    //             dueAt: null);
    //
    //         item.Activity.ChangeState(ActivityState.Canceled, now, $"Stress seed: cancellation reason {i}.");
    //         await repository.AddAsync(item, ct);
    //     }
    //
    //     // Failed — 20 items
    //     for (var i = 1; i <= 20; i++)
    //     {
    //         var category = _categories[i % _categories.Length];
    //         var priority = _priorities[i % _priorities.Length];
    //
    //         var item = Item.CreateTask(
    //             new ItemId(idGenerator.New()), owner,
    //             $"Seed Task Failed {i:D3}",
    //             $"Stress seed failed task number {i}. Deadline was missed.",
    //             category,
    //             now.AddDays(-(90 + i % 30)).AddHours(-(i % 24)),
    //             priority,
    //             dueAt: now.AddDays(-(i % 14 + 1)));
    //
    //         item.Activity.ChangeState(ActivityState.Failed, now, $"Stress seed: missed deadline on task {i}.");
    //         await repository.AddAsync(item, ct);
    //     }
    //
    //     await repository.SaveChangesAsync(ct);
    // }
    //
    // // ── Journals ───────────────────────────────────────────────────────────
    //
    // private async Task SeedJournalsAsync(DateTimeOffset now, string owner, CancellationToken ct)
    // {
    //     var utcDayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
    //
    //     for (var i = 1; i <= 50; i++)
    //     {
    //         var dayOffset  = -(i % 30);
    //         var hourOffset = 8 + i % 10;
    //         DateTimeOffset? untilAt = (i % 3 == 0)
    //             ? utcDayStart.AddDays(dayOffset).AddHours(hourOffset + 2)
    //             : null;
    //
    //         var journal = Item.CreateJournal(
    //             new ItemId(idGenerator.New()), owner,
    //             $"Development Log Entry {i:D2}",
    //             $"Stress seed journal entry {i}. Logged on relative day {dayOffset}.",
    //             now,
    //             new JournalExtension(
    //                 occurredAt: utcDayStart.AddDays(dayOffset).AddHours(hourOffset),
    //                 untilAt:    untilAt
    //             ));
    //
    //         await repository.AddAsync(journal, ct);
    //     }
    //
    //     await repository.SaveChangesAsync(ct);
    // }
    //
    // // ── Schedules ──────────────────────────────────────────────────────────
    //
    // private async Task SeedSchedulesAsync(DateTimeOffset now, string owner, string timezone, CancellationToken ct)
    // {
    //     var schedules = new[]
    //     {
    //         // 1. Daily standup
    //         MakeSchedule(owner, "Stress: Daily Standup", "Daily standup reminder.", now,
    //             ScheduleFrequencyType.Days, 1,
    //             new TimeOnly(9, 0), null,
    //             now.AddDays(-30), now.AddDays(60), timezone),
    //
    //         // 2. Weekly review
    //         MakeSchedule(owner, "Stress: Weekly Review", "End-of-week review session.", now,
    //             ScheduleFrequencyType.Weeks, 1,
    //             new TimeOnly(17, 0), null,
    //             now.AddDays(-30), now.AddDays(60), timezone),
    //
    //         // 3. Bi-daily check
    //         MakeSchedule(owner, "Stress: Bi-Daily Check", "Status check every two days.", now,
    //             ScheduleFrequencyType.Days, 2,
    //             new TimeOnly(10, 30), null,
    //             now.AddDays(-30), now.AddDays(60), timezone),
    //
    //         // 4. High-frequency trigger (scheduler stress test)
    //         MakeSchedule(owner, "Stress: 1-Min Trigger", "Fires every minute for scheduler testing.", now,
    //             ScheduleFrequencyType.Minutes, 1,
    //             null, null,
    //             now, now.AddHours(2), timezone,
    //             occurrenceCountLimit: 30),
    //
    //         // 5. Medium-frequency trigger
    //         MakeSchedule(owner, "Stress: 10-Min Trigger", "Fires every 10 minutes.", now,
    //             ScheduleFrequencyType.Minutes, 10,
    //             null, null,
    //             now.AddDays(-1), now.AddDays(1), timezone,
    //             occurrenceCountLimit: 20),
    //
    //         // 6. Hourly check
    //         MakeSchedule(owner, "Stress: Hourly Check", "Hourly working-hours reminder.", now,
    //             ScheduleFrequencyType.Hours, 1,
    //             null, null,
    //             now.AddDays(-7), now.AddDays(14), timezone),
    //
    //         // 7. Monthly report
    //         MakeSchedule(owner, "Stress: Monthly Report", "Monthly reporting run.", now,
    //             ScheduleFrequencyType.Days, 30,
    //             new TimeOnly(8, 0), null,
    //             now.AddDays(-60), now.AddDays(120), timezone),
    //
    //         // 8. Ended schedule (active window already closed)
    //         MakeSchedule(owner, "Stress: Expired Schedule", "Schedule whose active window is in the past.", now,
    //             ScheduleFrequencyType.Days, 1,
    //             new TimeOnly(9, 0), null,
    //             now.AddDays(-30), now.AddDays(-1), timezone),
    //     };
    //
    //     foreach (var s in schedules)
    //         await repository.AddAsync(s, ct);
    //
    //     await repository.SaveChangesAsync(ct);
    // }
    //
    // private Item MakeSchedule(
    //     string owner, string title, string description, DateTimeOffset now,
    //     ScheduleFrequencyType frequencyType, int frequencyInterval,
    //     TimeOnly? startTime, TimeOnly? endTime,
    //     DateTimeOffset activeFrom, DateTimeOffset activeUntil,
    //     string timezone,
    //     int? occurrenceCountLimit = null)
    // {
    //     return Item.CreateSchedule(
    //         new ItemId(idGenerator.New()), owner,
    //         title, description,
    //         now,
    //         new ScheduleExtension(
    //             targetItemId:         null,
    //             frequencyType:        frequencyType,
    //             frequencyInterval:    frequencyInterval,
    //             lookaheadLimit:       1,
    //             occurrenceCountLimit: occurrenceCountLimit,
    //             misfirePolicy:        ScheduleMisfirePolicy.Skip,
    //             occurrenceTtl:        null,
    //             actionType:           ScheduleActionType.None,
    //             payload:              null,
    //             startTime:            startTime,
    //             endTime:              endTime,
    //             activeFromUtc:        activeFrom,
    //             activeUntilUtc:       activeUntil,
    //             byDay:                null,
    //             byMonthDay:           null,
    //             byMonth:              null,
    //             timezone:             timezone
    //         ));
    // }
    //
    // // ── Helpers ────────────────────────────────────────────────────────────
    //
    // private void AddCompletedSession(
    //     Item item,
    //     DateTimeOffset startedAt,
    //     TimeSpan duration,
    //     string? objective,
    //     SessionEfficiencyType efficiency,
    //     SessionConcentrationType concentration,
    //     string? summary)
    // {
    //     item.Activity.StartSession(
    //         sessionId:         idGenerator.New(),
    //         segmentId:         idGenerator.New(),
    //         plannedDuration:   duration,
    //         objective:         objective,
    //         stopAutomatically: false,
    //         autoStopReason:    null,
    //         createdAtUtc:      startedAt);
    //
    //     item.Activity.TryGetSession!.Stop(
    //         nowUtc:        startedAt + duration,
    //         efficiency:    efficiency,
    //         concentration: concentration,
    //         summary:       summary);
    // }
}
