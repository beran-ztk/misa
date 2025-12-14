using System.Runtime.InteropServices;
using Misa.Domain.Entities;

namespace Misa.Domain.Main;

public class Description
{
    private Description() { }

    public Description( Guid guid, int typeId, string content)
    {
        EntityId = guid;
        TypeId = typeId;
        Content = content;
    }
    // Member
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public int TypeId { get; set; }
    public string Content { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DescriptionTypes Type { get; set; }
}
