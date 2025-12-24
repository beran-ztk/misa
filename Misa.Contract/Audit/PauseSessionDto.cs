namespace Misa.Contract.Audit;

public class PauseSessionDto
{
    public Guid EntityId { get; set; }
    public string? PauseReason { get; set; }

    public PauseSessionDto( Guid entityId, string? pauseReason )
    {
        EntityId = entityId;
        PauseReason = pauseReason;
    }
}