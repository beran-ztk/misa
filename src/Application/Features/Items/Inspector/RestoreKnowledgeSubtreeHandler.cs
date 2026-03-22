using System;
using System.Threading;
using System.Threading.Tasks;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;

namespace Misa.Application.Features.Items.Inspector;

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
