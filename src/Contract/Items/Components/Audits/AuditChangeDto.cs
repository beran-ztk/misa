namespace Misa.Contract.Items.Components.Audits;

public sealed record AuditChangeDto(
    Guid Id,
    Guid ItemId,
    string ChangeType,
    string? ValueBefore,
    string? ValueAfter,
    string? Reason,
    DateTimeOffset CreatedAtUtc
);
