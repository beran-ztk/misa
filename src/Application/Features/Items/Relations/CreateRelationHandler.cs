using Misa.Core.Persistence;
using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Relations;

namespace Misa.Core.Features.Items.Relations;

public sealed record CreateRelationCommand(
    Guid SourceItemId,
    Guid TargetItemId,
    RelationType RelationType
);

public sealed class CreateRelationHandler(
    ItemRepository repository)
{
    public async Task HandleAsync(CreateRelationCommand command, CancellationToken ct)
    {
        if (command.SourceItemId == command.TargetItemId)
            throw new DomainValidationException("targetItemId", "self_reference", "An entry cannot reference itself.");

        var source = await repository.TryGetItemAsync(command.SourceItemId);
        if (source is null || source.IsDeleted)
            throw new DomainValidationException("sourceItemId", "not_found", "Source item not found.");

        var target = await repository.TryGetItemAsync(command.TargetItemId);
        if (target is null || target.IsDeleted)
            throw new DomainValidationException("targetItemId", "not_found", "Target item not found.");

        if (await repository.RelationExistsAsync(command.SourceItemId, command.TargetItemId, ct))
            throw new DomainValidationException("targetItemId", "duplicate_relation", "A relation between these entries already exists.");

        var relation = new ItemRelation(
            command.SourceItemId,
            command.TargetItemId,
            command.RelationType
        );

        await repository.AddRelationAsync(relation, ct);
        await repository.SaveChangesAsync(ct);
    }
}
