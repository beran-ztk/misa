namespace Misa.Contract.Audit;

public class SessionSegmentDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }

    public string? PauseReason { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
}