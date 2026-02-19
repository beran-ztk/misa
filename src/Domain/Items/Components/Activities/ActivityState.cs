namespace Misa.Domain.Items.Components.Activities;

public enum ActivityState
{
    Draft,
    Undefined,

    InProgress,
    Active,
    Paused,
    Pending,

    WaitForResponse,

    Done,
    Canceled,
    Failed,
    Expired
}

public static class StateTransitions
{
    private static readonly Dictionary<ActivityState, ActivityState[]> Allowed = new()
    {
        [ActivityState.Draft] =
            [
                ActivityState.Undefined,
                ActivityState.WaitForResponse,
                ActivityState.Done,
                ActivityState.Canceled,
                ActivityState.Failed
            ],
        [ActivityState.Undefined] =
            [
                ActivityState.Draft,
                ActivityState.WaitForResponse,
                ActivityState.Done,
                ActivityState.Canceled,
                ActivityState.Failed
            ],
        [ActivityState.InProgress] =
            [
                ActivityState.WaitForResponse,
                ActivityState.Done,
                ActivityState.Canceled,
                ActivityState.Failed
            ],
        [ActivityState.Pending] =
            [
                ActivityState.WaitForResponse,
                ActivityState.Done,
                ActivityState.Canceled,
                ActivityState.Failed
            ],
        [ActivityState.WaitForResponse] =
            [
                ActivityState.InProgress,
                ActivityState.Done,
                ActivityState.Canceled,
                ActivityState.Failed
            ],
        [ActivityState.Done] =
            [
                ActivityState.InProgress
            ],
        [ActivityState.Canceled] =
            [
                ActivityState.InProgress
            ],
        [ActivityState.Failed] =
            [
                ActivityState.InProgress
            ],
        [ActivityState.Expired] =
            [
                ActivityState.InProgress
            ]
    };
    
    public static IReadOnlyCollection<ActivityState> From(ActivityState state)
        => Allowed.TryGetValue(state, out var next)
            ? next
            : [];

    public static ActivityState GetEnumFromId(int stateId)
    {
        if (!Enum.IsDefined(typeof(ActivityState), stateId))
            throw new InvalidCastException($"{stateId} does not Exist");

        return (ActivityState)stateId;
    }
}