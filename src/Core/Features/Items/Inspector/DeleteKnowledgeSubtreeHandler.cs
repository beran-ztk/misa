using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Items.Inspector;

public record DeleteKnowledgeSubtreeCommand(Guid[] Ids);

public sealed class DeleteKnowledgeSubtreeHandler(ItemRepository repository)
{
    public async Task HandleAsync(DeleteKnowledgeSubtreeCommand command)
    {
        foreach (var id in command.Ids)
        {
            var item = await repository.TryGetItemAsync(id);
            item?.Delete();
        }

        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
