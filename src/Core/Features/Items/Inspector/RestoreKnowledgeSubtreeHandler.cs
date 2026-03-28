using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Common.Abstractions.Time;

namespace Misa.Core.Features.Items.Inspector;

public record RestoreKnowledgeSubtreeCommand(Guid[] Ids);

public sealed class RestoreKnowledgeSubtreeHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(RestoreKnowledgeSubtreeCommand command)
    {
        foreach (var id in command.Ids)
        {
            var item = await repository.TryGetItemAsync(id);
            item?.Restore(timeProvider.UtcNow);
        }

        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
