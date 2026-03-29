namespace Misa.Domain;

public enum Kind
{
    Topic,
    Note,
    Quest
}

public sealed class Item
{
    private Item() { } // EF

    private Item(
        Guid? parentId,
        Kind kind,
        string title)
    {
        Id = Guid.NewGuid();
        ParentId = parentId;
        Kind = kind;
        Title = title;
    }
    
    // Fields + Properties
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public Kind Kind { get; init; }
    public string Title { get; private set; }
    
    public Note? Note { get; private set; }
    public Quest? Quest { get; private set; }

    public static Item CreateTopic(Guid? parentId, string title)
    {
        var item = new Item(parentId, Kind.Topic, title);
        return item;
    }
    public static Item CreateNote(Guid parentId, string title, string content)
    {
        var item = new Item(parentId, Kind.Note, title);
        item.Note = new Note(item.Id, content);
        return item;
    }
    public static Item CreateQuest(Guid parentId, string title)
    {
        var item = new Item(parentId, Kind.Quest, title);
        item.Quest = new Quest(item.Id);
        return item;
    }
}