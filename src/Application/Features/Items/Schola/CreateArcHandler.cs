using JasperFx.Blocks;
using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Activity;
using Misa.Domain.Items;

namespace Misa.Application.Features.Items.Schola;

public sealed record CreateArcCommand(
    string Title,
    string? Description,
    ActivityPriorityDto ActivityPriorityDto,
    string? Objective
);

public sealed class CreateArcHandler(
    IItemRepository repository, 
    ITimeProvider timeProvider, 
    IIdGenerator idGenerator,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(CreateArcCommand command)
    {
        var arc = Item.CreateArc(
            id: new ItemId(idGenerator.New()), 
            ownerId: currentUser.Id,
            title: command.Title, 
            description: command.Description, 
            createdAtUtc: timeProvider.UtcNow,
            priority: command.ActivityPriorityDto.ToDomain(),
            objective: command.Objective,
            dueAt: null
        );

        await repository.AddAsync(arc, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}