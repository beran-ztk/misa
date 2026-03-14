namespace Misa.Domain.Items.Components.Audits.Changes;

public class AuditChange
{
    private AuditChange() { }
    public AuditChange(
        Guid id,
        Guid subjectId,
        ChangeType changeType,
        string? before,
        string? after,
        string? reason,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        SubjectId = subjectId;
        ChangeType = changeType;
        ValueBefore = before;
        ValueAfter = after;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; init; }
    public Guid SubjectId { get; init; }

    public ChangeType ChangeType { get; init; }

    public string? ValueBefore { get; init; }
    public string? ValueAfter { get; init; }

    public string? Reason { get;  init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

}