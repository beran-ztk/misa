using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Items.Relations;

public sealed record DeleteRelationCommand(Guid RelationId);

public sealed class DeleteRelationHandler(ItemRepository repository)
{
    public async Task HandleAsync(DeleteRelationCommand command, CancellationToken ct)
    {
        await repository.DeleteRelationAsync(command.RelationId, ct);
        await repository.SaveChangesAsync(ct);
    }
}
