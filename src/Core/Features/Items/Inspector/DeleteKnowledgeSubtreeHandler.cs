using Misa.Core.Abstractions.Persistence;
using Misa.Core.Abstractions.Time;

namespace Misa.Core.Features.Items.Inspector;

public record DeleteKnowledgeSubtreeCommand(Guid[] Ids);

public sealed class DeleteKnowledgeSubtreeHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(DeleteKnowledgeSubtreeCommand command)
    {
        foreach (var id in command.Ids)
        {
            var item = await repository.TryGetItemAsync(id);
            item?.Delete(timeProvider.UtcNow);
        }

        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
