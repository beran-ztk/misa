namespace Misa.Domain.Features.Audit;

public class AuditChange
{
    private AuditChange() { }
    public AuditChange(
        Guid id,
        Guid entityId,
        ChangeType changeType,
        string? before,
        string? after,
        string? reason,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        EntityId = entityId;
        ChangeType = changeType;
        ValueBefore = before;
        ValueAfter = after;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; init; }
    public Guid EntityId { get; init; }

    public ChangeType ChangeType { get; init; }

    public string? ValueBefore { get; init; }
    public string? ValueAfter { get; init; }

    public string? Reason { get;  init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

}