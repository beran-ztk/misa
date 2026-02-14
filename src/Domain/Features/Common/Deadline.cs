namespace Misa.Domain.Features.Common;

public sealed class Deadline
{
    private Deadline () { }

    public Deadline(Guid itemId, DateTimeOffset dueAt, DateTimeOffset createdAt)
    {
        ItemId = itemId;
        DueAt = dueAt;
        CreatedAt = createdAt;
    }

    public Guid ItemId { get; init; }
    public DateTimeOffset DueAt { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    
    public void SetDeadline(DateTimeOffset dueAt) => DueAt = dueAt;
}