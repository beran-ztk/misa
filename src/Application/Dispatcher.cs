using Misa.Domain;

namespace Misa.Application;

public sealed class Dispatcher(
    CreateItemHandler createItemHandler,
    GetItemHandler getItemHandler,
    UpdateItemHandler updateItemHandler)
{
    // Create
    public Task<Item?> SendAsync(CreateTopicRequest r) => createItemHandler.Handle(r);
    public Task<Item?> SendAsync(CreateNoteRequest r)  => createItemHandler.Handle(r);
    public Task<Item?> SendAsync(CreateQuestRequest r) => createItemHandler.Handle(r);
    
    // Get
    public Task<List<Item>> GetAsync(GetItemsRequest r) => getItemHandler.Handle(r);
    
    // Update
    public Task<bool> UpdateAsync(UpdateTitleRequest r) => updateItemHandler.HandleAsync(r);
}
