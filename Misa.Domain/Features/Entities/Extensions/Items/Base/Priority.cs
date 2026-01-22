namespace Misa.Domain.Features.Entities.Extensions.Items.Base;

public class Priority
{
    private Priority() { }

    public Priority(
        int id,
        string name,
        int sortOrder,
        string? synopsis = null
        )
    {
        Id = id;
        Name = name;
        Synopsis = synopsis;
        SortOrder = sortOrder;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Synopsis { get; private set; }
    public int SortOrder { get; private set; }
}
