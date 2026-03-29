using Misa.Domain;

namespace Misa.Application;

public record CreateTopicCommand(Guid? ParentId, string Title);
public record CreateNoteCommand(Guid ParentId, string Title, string Content);
public record CreateQuestCommand(Guid ParentId, string Title);

public sealed class CreateItemHandler(Repository repository)
{
    private Task Save(Item item) => repository.AddAsync(item);
    
    // Create Topic
    public async Task Handle(CreateTopicCommand createTopicCommand)
    {
        var item = Item.CreateTopic(createTopicCommand.ParentId, createTopicCommand.Title);
        await Save(item);
    }
    
    // Create Note
    public async Task Handle(CreateNoteCommand createNoteCommand)
    {
        var item = Item.CreateNote(createNoteCommand.ParentId, createNoteCommand.Title, createNoteCommand.Content);
        await Save(item);
    }
    
    // Create Quest
    public async Task Handle(CreateQuestCommand createQuestCommand)
    {
        var item = Item.CreateQuest(createQuestCommand.ParentId, createQuestCommand.Title);
        await Save(item);
    }
}