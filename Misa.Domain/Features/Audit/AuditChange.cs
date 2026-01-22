namespace Misa.Domain.Features.Audit;

public class AuditChange
{
    private AuditChange() { }
    public AuditChange(
        Guid entityId,
        ChangeType changeType,
        string? before,
        string? after,
        string? reason,
        DateTimeOffset createdAtUtc)
    {
        EntityId = entityId;
        ChangeType = changeType;
        ValueBefore = before;
        ValueAfter = after;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid EntityId { get; init; }

    public ChangeType ChangeType { get; init; }

    public string? ValueBefore { get; init; }
    public string? ValueAfter { get; init; }

    public string? Reason { get;  init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

}