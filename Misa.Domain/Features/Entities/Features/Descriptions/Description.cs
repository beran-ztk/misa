
namespace Misa.Domain.Features.Entities.Features.Descriptions;

public class Description
{
    private Description() { }
    public static Description Create(Guid id, string content)
    {
        return new Description
        {
            EntityId = id,
            Content = content
        };
    }
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string Content { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}