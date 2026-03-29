using Misa.Core.Persistence;
using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Relations;

namespace Misa.Core.Features.Items.Relations;

public sealed record UpdateRelationCommand(Guid RelationId, RelationType RelationType);

public sealed class UpdateRelationHandler(ItemRepository repository)
{
    public async Task HandleAsync(UpdateRelationCommand command, CancellationToken ct)
    {
        var relation = await repository.TryGetRelationAsync(command.RelationId, ct)
            ?? throw new DomainNotFoundException("relation.not.found", command.RelationId.ToString());

        relation.ChangeType(command.RelationType);
        await repository.SaveChangesAsync(ct);
    }
}
