using Misa.Domain;

namespace Misa.Application;

public sealed class Dispatcher(
    CreateItemHandler createItemHandler,
    GetItemHandler getItemHandler)
{
    // Create
    public Task SendAsync(CreateTopicRequest r) => createItemHandler.Handle(r);
    public Task SendAsync(CreateNoteRequest r)  => createItemHandler.Handle(r);
    public Task SendAsync(CreateQuestRequest r) => createItemHandler.Handle(r);
    
    // Get
    public Task<List<Item>> GetAsync(GetItemsRequest r) => getItemHandler.Handle(r);
}
