using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Audits.Changes;

namespace Misa.Domain.Items.Components.Activities;

public sealed class ItemActivity : DomainEventEntity
{
    private ItemActivity() { } // EF

    public ItemActivity(
        ItemId id,
        ActivityState state,
        ActivityPriority priority,
        string? objective,
        string? summary,
        DateTimeOffset? dueAt)
    {
        Id = id;
        State = state;
        Priority = priority;
        Objective = objective;
        Summary = summary;
        DueAt = dueAt;
    }
    
    // Fields + Properties
    public ItemId Id { get; init; }
    public ActivityState State { get; private set; }
    public ActivityPriority Priority { get; private set; }
    public string? Objective { get; private set; }
    public string? Summary { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    
    // Components
    public Item Item { get; private set; } = null!;
    public ICollection<Session> Sessions { get; private set; } = new List<Session>();
    
    // Derived Properties
    public Session? TryGetSession => Sessions.FirstOrDefault(s => s.State != SessionState.Ended);
    public bool HasActiveSession => TryGetSession != null;

    public bool CanStartSession
        => State == ActivityState.Open && TryGetSession == null;
    
    // Mutators
    public void SetDeadline(DateTimeOffset? deadline, DateTimeOffset nowUtc)
    {
        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.Deadline, DueAt?.ToString("O"), deadline?.ToString("O"), null));
        DueAt = deadline;
        Item.Touch(nowUtc);
    }

    public void ChangeState(ActivityState state, DateTimeOffset nowUtc, string? reason = null)
    {
        if (State == state)
            return;

        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.State, State.ToString(), state.ToString(), reason));
        State = state;
        Item.Touch(nowUtc);
    }

    public void ChangePriority(ActivityPriority priority, DateTimeOffset nowUtc)
    {
        if (Priority == priority)
            return;

        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.Priority, Priority.ToString(), priority.ToString(), null));
        Priority = priority;
        Item.Touch(nowUtc);
    }

    public void StartSession(
        Guid sessionId,
        Guid segmentId,
        TimeSpan? plannedDuration,
        string? objective,
        bool stopAutomatically,
        string? autoStopReason,
        DateTimeOffset createdAtUtc)
    {
        var session = Session.Start(
            sessionId: sessionId,
            segmentId: segmentId,
            plannedDuration: plannedDuration,
            objective: objective,
            stopAutomatically: stopAutomatically,
            autoStopReason: autoStopReason,
            createdAtUtc: createdAtUtc);

        Sessions.Add(session);
        Item.Touch(createdAtUtc);
    }

    public void PauseCurrentSession(string? reason, DateTimeOffset nowUtc)
    {
        TryGetSession!.Pause(reason, nowUtc);
        Item.Touch(nowUtc);
    }

    public void ContinueCurrentSession(Guid segmentId, DateTimeOffset nowUtc)
    {
        TryGetSession!.Continue(segmentId, nowUtc);
        Item.Touch(nowUtc);
    }

    public void StopCurrentSession(
        DateTimeOffset nowUtc,
        SessionEfficiencyType efficiency,
        SessionConcentrationType concentration,
        string? summary)
    {
        TryGetSession!.Stop(nowUtc, efficiency, concentration, summary);
        Item.Touch(nowUtc);
    }
}