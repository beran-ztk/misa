namespace Misa.Domain.Dictionaries.Items;

public static class StateTransitions
{
    private static readonly Dictionary<ItemStates, ItemStates[]> Allowed = new()
    {
        [ItemStates.Draft] =
        [
            ItemStates.Undefined,
            ItemStates.WaitForResponse,
            ItemStates.Done,
            ItemStates.Canceled,
            ItemStates.Failed
        ],
        [ItemStates.Undefined] =
        [
            ItemStates.Draft,
            ItemStates.WaitForResponse,
            ItemStates.Done,
            ItemStates.Canceled,
            ItemStates.Failed
        ]
    };
    
    public static IReadOnlyCollection<ItemStates> From(ItemStates state)
        => Allowed.TryGetValue(state, out var next)
            ? next
            : [];

    public static ItemStates GetEnumFromId(int stateId)
    {
        if (!Enum.IsDefined(typeof(ItemStates), stateId))
            throw new InvalidCastException($"{stateId} does not Exist");

        return (ItemStates)stateId;
    }
}