using Misa.Domain;

namespace Misa.Application;

public record CreateTopicRequest(Guid? ParentId, string Title);
public record CreateNoteRequest(Guid ParentId, string Title, string Content);
public record CreateQuestRequest(Guid ParentId, string Title);

public sealed class CreateItemHandler(Repository repository)
{
    private Task Save(Item item) => repository.AddAsync(item);
    
    // Create Topic
    public async Task Handle(CreateTopicRequest createTopicRequest)
    {
        var item = Item.CreateTopic(createTopicRequest.ParentId, createTopicRequest.Title);
        await Save(item);
    }
    
    // Create Note
    public async Task Handle(CreateNoteRequest createNoteRequest)
    {
        var item = Item.CreateNote(createNoteRequest.ParentId, createNoteRequest.Title, createNoteRequest.Content);
        await Save(item);
    }
    
    // Create Quest
    public async Task Handle(CreateQuestRequest createQuestRequest)
    {
        var item = Item.CreateQuest(createQuestRequest.ParentId, createQuestRequest.Title);
        await Save(item);
    }
}