using System.ComponentModel.DataAnnotations;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Common.Exceptions;
using Misa.Application.Features.Entities.Extensions.Items.Features.Deadlines.Events;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Wolverine;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Deadlines.Commands;

public sealed class UpsertItemDeadlineHandler(IItemRepository repository, IMessageBus bus)
{
    public async Task Handle(UpsertItemDeadlineCommand command, CancellationToken ct = default)
    {
        if (command.ItemId == Guid.Empty)
            throw new ValidationException("ItemId must not be empty.");

        if (command.DueAt == default)
            throw new ValidationException("DueAt must be specified.");

        var item = await repository.TryGetItemAsync(command.ItemId, CancellationToken.None);
        if (item is null)
            throw NotFoundException.For("Item", command.ItemId);

        var dueAtUtc = command.DueAt.ToUniversalTime();
        if (dueAtUtc < DateTimeOffset.UtcNow)
            throw new ValidationException("DueAt must not be in the past."); 
        
        var deadline = await repository.TryGetScheduledDeadlineForItemAsync(command.ItemId, ct);
        
        if (deadline is null)
        {
            await repository.AddDeadlineAsync(new ScheduledDeadline(command.ItemId, dueAtUtc));
        }
        else
        {
            deadline.RescheduleDeadlineAndAuditChanges(dueAtUtc);
        }
        
        await repository.SaveChangesAsync(ct);

        await bus.PublishAsync(new ItemDeadlineUpsertedEvent(command.ItemId, dueAtUtc));
    }
}