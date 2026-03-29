using Misa.Core.Persistence;

namespace Misa.Core.Features.Items.Inspector;

public record RestoreKnowledgeSubtreeCommand(Guid[] Ids);

public sealed class RestoreKnowledgeSubtreeHandler(ItemRepository repository)
{
    public async Task HandleAsync(RestoreKnowledgeSubtreeCommand command)
    {
        foreach (var id in command.Ids)
        {
            var item = await repository.TryGetItemAsync(id);
            item?.Restore();
        }

        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
