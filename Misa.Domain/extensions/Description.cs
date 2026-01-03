namespace Misa.Domain.Extensions;

public class Description
{
    private Description() { }

    public Description( Guid guid, int typeId, string content, DateTimeOffset? createdAtUtc)
    {
        EntityId = guid;
        TypeId = typeId;
        Content = content;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
    }
    // Member
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public int TypeId { get; set; }
    public string Content { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DescriptionTypes Type { get; set; }
}