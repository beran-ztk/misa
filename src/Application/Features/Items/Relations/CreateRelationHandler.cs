using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Relations;
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
