using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Chronicle.Journals;
using Misa.Domain.Items.Components.Schedules;
using Misa.Domain.Items.Components.Tasks;

namespace Misa.Application.Features.Dev;

public record SeedDevDataCommand;

public sealed class SeedDevDataHandler(
    IItemRepository repository,
    ITimeProvider timeProvider,
    IIdGenerator idGenerator,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(SeedDevDataCommand command, CancellationToken ct)
    {
        var now    = timeProvider.UtcNow;
        var owner  = currentUser.Id;
        var tz     = currentUser.Timezone;

        await SeedTasksAsync(now, owner, ct);
        await SeedJournalsAsync(now, owner, ct);
        await SeedSchedulesAsync(now, owner, tz, ct);
    }

    // ── Tasks ──────────────────────────────────────────────────────────────

    private async Task SeedTasksAsync(DateTimeOffset now, string owner, CancellationToken ct)
    {
        // 1. Open — upcoming deadline, high priority
        var t1 = Item.CreateTask(
            new ItemId(idGenerator.New()), owner,
            "Review quarterly objectives",
            "Go through last quarter's goals and measure completion.",
            TaskCategory.Work, now.AddDays(-5), ActivityPriority.High,
            dueAt: now.AddDays(2));
        await repository.AddAsync(t1, ct);

        // 2. Open — no deadline, with a completed session
        var t2 = Item.CreateTask(
            new ItemId(idGenerator.New()), owner,
            "Research productivity frameworks",
            "Explore GTD, Zettelkasten, and time-boxing approaches.",
            TaskCategory.Personal, now.AddDays(-3), ActivityPriority.Medium,
            dueAt: null);
        AddCompletedSession(t2, now.AddDays(-1), TimeSpan.FromMinutes(42),
            "Initial research sweep",
            SessionEfficiencyType.SteadyOutput, SessionConcentrationType.Focused,
            "Covered GTD basics and Zettelkasten core principles. Need to dig deeper into time-boxing.");
        await repository.AddAsync(t2, ct);

        // 3. Open — deadline tomorrow, high priority
        var t3 = Item.CreateTask(
            new ItemId(idGenerator.New()), owner,
            "Prepare sprint planning notes",
            null,
            TaskCategory.Work, now.AddDays(-2), ActivityPriority.High,
            dueAt: now.AddDays(1));
        await repository.AddAsync(t3, ct);

        // 4. Open — low priority, no deadline
        var t4 = Item.CreateTask(
            new ItemId(idGenerator.New()), owner,
            "Organize digital workspace",
            "Clean up folder structure, archive stale files, update bookmarks.",
            TaskCategory.Personal, now.AddDays(-7), ActivityPriority.Low,
            dueAt: null);
        await repository.AddAsync(t4, ct);

        // 5. Open — deadline in a few hours (for deadline stress testing)
        var t5 = Item.CreateTask(
            new ItemId(idGenerator.New()), owner,
            "Complete learning module",
            "Finish the current online course unit before tonight's session.",
            TaskCategory.School, now.AddDays(-1), ActivityPriority.Medium,
            dueAt: now.AddHours(6));
        await repository.AddAsync(t5, ct);

        // 6. Open — medium priority, no deadline
        var t6 = Item.CreateTask(
            new ItemId(idGenerator.New()), owner,
            "Draft system architecture diagram",
            "Sketch the major components and boundaries for the new service.",
            TaskCategory.Work, now.AddHours(-18), ActivityPriority.Medium,
            dueAt: null);
        await repository.AddAsync(t6, ct);

        // 7. Done — with completed session
        var t7 = Item.CreateTask(
            new ItemId(idGenerator.New()), owner,
            "Set up development environment",
            "Install tooling, configure IDE and dotnet SDK.",
            TaskCategory.Work, now.AddDays(-10), ActivityPriority.Medium,
            dueAt: null);
        AddCompletedSession(t7, now.AddDays(-8), TimeSpan.FromMinutes(105),
            "Environment setup run",
            SessionEfficiencyType.HighOutput, SessionConcentrationType.Focused,
            "Got everything running. IDE, SDK, Postgres and Avalonia previewer all working.");
        t7.Activity.ChangeState(ActivityState.Done);
        await repository.AddAsync(t7, ct);

        // 8. Done — quick item
        var t8 = Item.CreateTask(
            new ItemId(idGenerator.New()), owner,
            "Write project README",
            null,
            TaskCategory.Work, now.AddDays(-6), ActivityPriority.Medium,
            dueAt: null);
        t8.Activity.ChangeState(ActivityState.Done);
        await repository.AddAsync(t8, ct);

        // 9. Cancelled
        var t9 = Item.CreateTask(
            new ItemId(idGenerator.New()), owner,
            "Schedule team offsite event",
            "Research venues, poll team availability.",
            TaskCategory.Work, now.AddDays(-15), ActivityPriority.Low,
            dueAt: null);
        t9.Activity.ChangeState(ActivityState.Canceled, "Postponed to next quarter — budget freeze.");
        await repository.AddAsync(t9, ct);

        // 10. Failed — overdue deadline
        var t10 = Item.CreateTask(
            new ItemId(idGenerator.New()), owner,
            "Submit expense report",
            "Monthly expenses must be submitted before the 15th.",
            TaskCategory.Work, now.AddDays(-14), ActivityPriority.Urgent,
            dueAt: now.AddDays(-3));
        t10.Activity.ChangeState(ActivityState.Failed, "Missed the finance submission window.");
        await repository.AddAsync(t10, ct);

        await repository.SaveChangesAsync(ct);
    }

    // ── Journals ───────────────────────────────────────────────────────────

    private async Task SeedJournalsAsync(DateTimeOffset now, string owner, CancellationToken ct)
    {
        var utcDayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);

        var journals = new[]
        {
            (title: "Morning planning notes",
                desc:  "Blocked out focus time and reviewed today's priorities. Goal is to push the sprint review prep forward.",
                at:    utcDayStart.AddDays(-3).AddHours(8).AddMinutes(15),
                until: (DateTimeOffset?)null),

            (title: "Retrospective insights",
                desc:  "Team retro went well. Key themes: communication lag on blocked tickets, and need for clearer acceptance criteria.",
                at:    utcDayStart.AddDays(-2).AddHours(16).AddMinutes(30),
                until: (DateTimeOffset?)null),

            (title: "Deep work session log",
                desc:  "Three uninterrupted hours on the domain model. Got the new extension wired in and tests passing.",
                at:    utcDayStart.AddDays(-1).AddHours(10),
                until: utcDayStart.AddDays(-1).AddHours(13)),

            (title: "Weekly planning",
                desc:  "Set intentions for the week. Focusing on reducing WIP and finishing in-flight tasks before picking anything new.",
                at:    utcDayStart.AddDays(-4).AddHours(9),
                until: (DateTimeOffset?)null),

            (title: "End of day notes",
                desc:  "Wrapped up the deadline feature. A few edge cases still to cover but core path works end to end.",
                at:    utcDayStart.AddHours(17).AddMinutes(45),
                until: (DateTimeOffset?)null),
        };

        foreach (var (title, desc, at, until) in journals)
        {
            var journal = Item.CreateJournal(
                new ItemId(idGenerator.New()), owner,
                title, desc,
                now,
                new JournalExtension(
                    occurredAt: at,
                    untilAt:    until
                ));
            await repository.AddAsync(journal, ct);
        }

        await repository.SaveChangesAsync(ct);
    }

    // ── Schedules ──────────────────────────────────────────────────────────

    private async Task SeedSchedulesAsync(DateTimeOffset now, string owner, string timezone, CancellationToken ct)
    {
        // Daily morning review — active from 7 days ago, no end, every day
        var s1 = Item.CreateSchedule(
            new ItemId(idGenerator.New()), owner,
            "Daily morning review",
            "Review open tasks and set daily intentions.",
            now,
            new ScheduleExtension(
                targetItemId:        null,
                frequencyType:       ScheduleFrequencyType.Days,
                frequencyInterval:   1,
                lookaheadLimit:      1,
                occurrenceCountLimit: null,
                misfirePolicy:       ScheduleMisfirePolicy.Skip,
                occurrenceTtl:       null,
                actionType:          ScheduleActionType.None,
                payload:             null,
                startTime:           new TimeOnly(8, 0),
                endTime:             null,
                activeFromUtc:       now.AddDays(-7),
                activeUntilUtc:      null,
                byDay:               null,
                byMonthDay:          null,
                byMonth:             null,
                timezone:            timezone
            ));
        await repository.AddAsync(s1, ct);

        // Weekly retrospective — every 7 days, starts 14 days ago
        var s2 = Item.CreateSchedule(
            new ItemId(idGenerator.New()), owner,
            "Weekly retrospective",
            "Review the week's outcomes and prepare lessons-learned notes.",
            now,
            new ScheduleExtension(
                targetItemId:        null,
                frequencyType:       ScheduleFrequencyType.Weeks,
                frequencyInterval:   1,
                lookaheadLimit:      1,
                occurrenceCountLimit: null,
                misfirePolicy:       ScheduleMisfirePolicy.RunOnce,
                occurrenceTtl:       null,
                actionType:          ScheduleActionType.None,
                payload:             null,
                startTime:           new TimeOnly(16, 30),
                endTime:             null,
                activeFromUtc:       now.AddDays(-14),
                activeUntilUtc:      null,
                byDay:               null,
                byMonthDay:          null,
                byMonth:             null,
                timezone:            timezone
            ));
        await repository.AddAsync(s2, ct);

        // Quick-fire test schedule — every 5 minutes, limited to 10 occurrences
        var s3 = Item.CreateSchedule(
            new ItemId(idGenerator.New()), owner,
            "Scheduler smoke test (5 min)",
            "Dev-only schedule for testing scheduler behavior quickly.",
            now,
            new ScheduleExtension(
                targetItemId:        null,
                frequencyType:       ScheduleFrequencyType.Minutes,
                frequencyInterval:   5,
                lookaheadLimit:      1,
                occurrenceCountLimit: 10,
                misfirePolicy:       ScheduleMisfirePolicy.Skip,
                occurrenceTtl:       null,
                actionType:          ScheduleActionType.None,
                payload:             null,
                startTime:           null,
                endTime:             null,
                activeFromUtc:       now,
                activeUntilUtc:      now.AddHours(1),
                byDay:               null,
                byMonthDay:          null,
                byMonth:             null,
                timezone:            timezone
            ));
        await repository.AddAsync(s3, ct);

        await repository.SaveChangesAsync(ct);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void AddCompletedSession(
        Item item,
        DateTimeOffset startedAt,
        TimeSpan duration,
        string? objective,
        SessionEfficiencyType efficiency,
        SessionConcentrationType concentration,
        string? summary)
    {
        item.Activity.StartSession(
            sessionId:        idGenerator.New(),
            segmentId:        idGenerator.New(),
            plannedDuration:  duration,
            objective:        objective,
            stopAutomatically: false,
            autoStopReason:   null,
            createdAtUtc:     startedAt);

        item.Activity.TryGetSession!.Stop(
            nowUtc:        startedAt + duration,
            efficiency:    efficiency,
            concentration: concentration,
            summary:       summary);
    }
}
