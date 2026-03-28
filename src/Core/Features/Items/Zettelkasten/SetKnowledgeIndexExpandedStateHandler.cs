using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record SetKnowledgeIndexExpandedStateCommand(Guid Id, bool IsExpanded);

public sealed class SetKnowledgeIndexExpandedStateHandler(IItemRepository repository)
{
    public async Task HandleAsync(SetKnowledgeIndexExpandedStateCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetKnowledgeIndexItemAsync(command.Id, ct);
        if (item?.KnowledgeIndex is null) return;

        item.SetKnowledgeIndexExpanded(command.IsExpanded);
        await repository.SaveChangesAsync(ct);
    }
}
