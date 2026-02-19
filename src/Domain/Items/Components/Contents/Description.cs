
namespace Misa.Domain.Features.Entities.Features;

public class Description
{
    private Description() { }

    public Description(Guid id, Guid entityId, string content)
    {
        Id = id;
        EntityId = entityId;
        Content = content;
    }
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public string Content { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    
    public void UpdateContent(string content)
    {
        Content = content;
    }
}