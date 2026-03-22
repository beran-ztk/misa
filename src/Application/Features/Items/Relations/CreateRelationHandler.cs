using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Relations;
using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Relations;

namespace Misa.Application.Features.Items.Relations;

public sealed record CreateRelationCommand(
    Guid SourceItemId,
    Guid TargetItemId,
    RelationTypeDto RelationType
);

public sealed class CreateRelationHandler(
    IItemRepository repository,
    ITimeProvider timeProvider,
    IIdGenerator idGenerator)
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
            idGenerator.New(),
            command.SourceItemId,
            command.TargetItemId,
            command.RelationType.ToDomain(),
            timeProvider.UtcNow
        );

        await repository.AddRelationAsync(relation, ct);
        await repository.SaveChangesAsync(ct);
    }
}
