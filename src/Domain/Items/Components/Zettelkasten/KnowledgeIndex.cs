namespace Misa.Domain.Items.Components.Zettelkasten;

public class KnowledgeIndex
{
    private KnowledgeIndex() { } // EF

    public KnowledgeIndex(ItemId id, ItemId? parentId)
    {
        Id = id;
        ParentId = parentId;
    }

    public ItemId Id { get; init; }
    public ItemId? ParentId { get; init; }
    public bool IsExpanded { get; private set; }

    public void SetExpanded(bool isExpanded) => IsExpanded = isExpanded;
}
