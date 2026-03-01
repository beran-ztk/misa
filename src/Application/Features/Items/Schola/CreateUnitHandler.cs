using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Activity;
using Misa.Domain.Items;

namespace Misa.Application.Features.Items.Schola;

public sealed record CreateUnitCommand(
    string Title,
    string? Description,
    ActivityPriorityDto ActivityPriorityDto,
    string? Objective
);

public sealed class CreateUnitHandler(
    IItemRepository repository, 
    ITimeProvider timeProvider, 
    IIdGenerator idGenerator,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(CreateUnitCommand command)
    {
        var arc = Item.CreateUnit(
            id: new ItemId(idGenerator.New()), 
            ownerId: currentUser.Id,
            title: command.Title, 
            description: command.Description, 
            createdAtUtc: timeProvider.UtcNow,
            priority: command.ActivityPriorityDto.ToDomain(),
            objective: command.Objective,
            dueAt: null,
            arcId: null
        );

        await repository.AddAsync(arc, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}