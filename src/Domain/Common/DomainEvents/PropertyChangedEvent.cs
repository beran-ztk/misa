using Misa.Domain.Features.Audit;

namespace Misa.Domain.Shared.DomainEvents;

public sealed record PropertyChangedEvent(
    Guid EntityId,
    ChangeType ChangeType,
    string? OldValue,
    string? NewValue,
    string? Reason
) : IDomainEvent;