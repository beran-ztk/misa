using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;

namespace Misa.Application.Features.Items.Inspector;

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
