namespace Misa.Domain.Entities.Extensions;

public class Description
{
    private Description() { }

    public Description( Guid guid, int typeId, string content, DateTimeOffset? createdAtUtc)
    {
        EntityId = guid;
        Content = content;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
    }
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string Content { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}