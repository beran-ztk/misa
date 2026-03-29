namespace Misa.Application;

public sealed class Dispatcher(CreateItemHandler items)
{
    public Task SendAsync(CreateTopicCommand cmd) => items.Handle(cmd);
    public Task SendAsync(CreateNoteCommand cmd)  => items.Handle(cmd);
    public Task SendAsync(CreateQuestCommand cmd) => items.Handle(cmd);
}
