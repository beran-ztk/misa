namespace Misa.Domain.Features.Entities.Extensions.Items.Base;

public static class StateTransitions
{
    private static readonly Dictionary<ItemState, ItemState[]> Allowed = new()
    {
        [ItemState.Draft] =
            [
                ItemState.Undefined,
                ItemState.WaitForResponse,
                ItemState.Done,
                ItemState.Canceled,
                ItemState.Failed
            ],
        [ItemState.Undefined] =
            [
                ItemState.Draft,
                ItemState.WaitForResponse,
                ItemState.Done,
                ItemState.Canceled,
                ItemState.Failed
            ],
        [ItemState.InProgress] =
            [
                ItemState.WaitForResponse,
                ItemState.Done,
                ItemState.Canceled,
                ItemState.Failed
            ],
        [ItemState.Pending] =
            [
                ItemState.WaitForResponse,
                ItemState.Done,
                ItemState.Canceled,
                ItemState.Failed
            ],
        [ItemState.WaitForResponse] =
            [
                ItemState.InProgress,
                ItemState.Done,
                ItemState.Canceled,
                ItemState.Failed
            ],
        [ItemState.Done] =
            [
                ItemState.InProgress
            ],
        [ItemState.Canceled] =
            [
                ItemState.InProgress
            ],
        [ItemState.Failed] =
            [
                ItemState.InProgress
            ],
        [ItemState.Expired] =
            [
                ItemState.InProgress
            ]
    };
    
    public static IReadOnlyCollection<ItemState> From(ItemState state)
        => Allowed.TryGetValue(state, out var next)
            ? next
            : [];

    public static ItemState GetEnumFromId(int stateId)
    {
        if (!Enum.IsDefined(typeof(ItemState), stateId))
            throw new InvalidCastException($"{stateId} does not Exist");

        return (ItemState)stateId;
    }
}