namespace Misa.Domain;

public sealed class Note
{
    private Note() { } // EF

    public Note(Guid itemId)
    {
        ItemId = itemId;
    }
    
    // Fields + Properties
    public Guid ItemId { get; init; }
    public string Content { get; set; } = string.Empty;
}