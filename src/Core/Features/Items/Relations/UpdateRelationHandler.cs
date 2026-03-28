using Misa.Contract.Items.Components.Relations;
using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Mappings;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Relations;

public sealed record UpdateRelationCommand(Guid RelationId, RelationTypeDto RelationType);

public sealed class UpdateRelationHandler(IItemRepository repository)
{
    public async Task HandleAsync(UpdateRelationCommand command, CancellationToken ct)
    {
        var relation = await repository.TryGetRelationAsync(command.RelationId, ct)
            ?? throw new DomainNotFoundException("relation.not.found", command.RelationId.ToString());

        relation.ChangeType(command.RelationType.ToDomain());
        await repository.SaveChangesAsync(ct);
    }
}
