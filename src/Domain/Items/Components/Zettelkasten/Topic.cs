namespace Misa.Domain.Items.Components.Zettelkasten;

public class Topic
{
    private Topic() {} // EF
    
    public Topic(ItemId id, ItemId? topicId)
    {
        Id = id;
        TopicId = topicId;
    }
    
    public ItemId Id { get; init; }
    public ItemId? TopicId { get; init; }
}