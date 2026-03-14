using Misa.Domain.Items.Components.Audits.Changes;

namespace Misa.Domain.Common.DomainEvents;

public sealed record PropertyChangedEvent(
    Guid ItemId,
    ChangeType ChangeType,
    string? OldValue,
    string? NewValue,
    string? Reason
) : IDomainEvent;