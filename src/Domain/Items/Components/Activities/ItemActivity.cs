using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Activities.Sessions;

namespace Misa.Domain.Items.Components.Activities;

public sealed class ItemActivity
{
    private ItemActivity() { } // EF

    public ItemActivity(
        ActivityState state,
        ActivityPriority priority,
        string? objective,
        string? summary,
        DateTimeOffset? dueAt)
    {
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
    public bool HasActiveSession 
        => State == ActivityState.Active;
    
    public bool CanStartSession
        => State 
            is ActivityState.Draft
            or ActivityState.Undefined
            or ActivityState.InProgress
            or ActivityState.Pending
            or ActivityState.WaitForResponse;
    
    // Mutator
    public void SetDeadline(DateTimeOffset? deadline) => DueAt = deadline;

    public void ChangeState(ActivityState state)
    {
        if (State == state)
            return;
        
        State = state;
    }
    public void ChangePriority(ActivityPriority priority)
    {
        if (Priority == priority)
            return;
        
        Priority = priority;
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
    }
}