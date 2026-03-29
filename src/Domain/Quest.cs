namespace Misa.Domain;

public enum State
{
    Open,
    Done
}

public sealed class Quest
{
    private Quest() { } // EF

    public Quest(Guid itemId)
    {
        ItemId = itemId;
    }
    
    // Fields + Properties
    public Guid ItemId { get; init; }
    public State State { get; private set; } = State.Open;
}