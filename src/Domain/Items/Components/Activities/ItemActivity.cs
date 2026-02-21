using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Activities.Sessions;

namespace Misa.Domain.Items.Components.Activities;

public sealed class ItemActivity
{
    private ItemActivity() { } // EF

    public ItemActivity(
        ActivityState state,
        ActivityPriority priority,
        DateTimeOffset? dueAt)
    {
        State = state;
        Priority = priority;
        DueAt = dueAt;
    }
    
    // Fields + Properties
    public ItemId Id { get; init; }
    public ActivityState State { get; init; }
    public ActivityPriority Priority { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    
    // Components
    public ICollection<Session> Sessions { get; private set; } = new List<Session>();
    
    // Derived Properties
    public bool HasActiveSession 
        => State == ActivityState.Active;
    
    public bool CanStartSession
        => State 
            is ActivityState.Draft
            or ActivityState.Undefined
            or ActivityState.InProgress
            or ActivityState.Pending
            or ActivityState.WaitForResponse;
}