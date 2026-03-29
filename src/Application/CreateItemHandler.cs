using Misa.Domain;

namespace Misa.Application;

public record CreateTopicRequest(Guid? ParentId, string Title);
public record CreateNoteRequest(Guid? ParentId, string Title);
public record CreateQuestRequest(Guid? ParentId, string Title);

public sealed class CreateItemHandler(Repository repository)
{
    private Task Save(Item item) => repository.AddAsync(item);
    
    // Create Topic
    public async Task<Item?> Handle(CreateTopicRequest createTopicRequest)
    {
        var item = Item.CreateTopic(createTopicRequest.ParentId, createTopicRequest.Title);
        await Save(item);
        
        return await repository.GetItemAsync(item.Id);
    }
    
    // Create Note
    public async Task<Item?> Handle(CreateNoteRequest createNoteRequest)
    {
        var item = Item.CreateNote(createNoteRequest.ParentId, createNoteRequest.Title);
        await Save(item);
        
        return await repository.GetItemAsync(item.Id);
    }
    
    // Create Quest
    public async Task<Item?> Handle(CreateQuestRequest createQuestRequest)
    {
        var item = Item.CreateQuest(createQuestRequest.ParentId, createQuestRequest.Title);
        await Save(item);
        
        return await repository.GetItemAsync(item.Id);
    }
}