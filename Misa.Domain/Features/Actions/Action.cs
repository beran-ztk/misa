namespace Misa.Domain.Features.Actions;

public class Action
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; set; }

    public int TypeId { get; set; }
    public ActionType Type { get; private set; } = null!;

    public string? ValueBefore { get; set; }
    public string? ValueAfter { get; set; }

    public string? Reason { get;  set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public static Action Create(Guid entityId, int typeId, string? before, string? after, string? reason, DateTimeOffset nowUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            TypeId = typeId,
            ValueBefore = before,
            ValueAfter = after,
            Reason = reason,
            CreatedAtUtc = nowUtc
        };
}