namespace Misa.Domain.Notifications;
public enum NotificationSourceKind
{
    SchedulerExecution,
    SessionPlannedDurationReached,
    ScheduleCreatedTask
}

public sealed class Notification
{
    private Notification() { } // EF Core

    public Notification(
        string title,
        string message,
        NotificationSourceKind sourceKind,
        Guid sourceId)
    {
        Id           = Guid.NewGuid();
        Title        = title;
        Message      = message;
        SourceKind   = sourceKind;
        SourceId     = sourceId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid                   Id             { get; private set; }
    public string                 Title          { get; private set; } = string.Empty;
    public string                 Message        { get; private set; } = string.Empty;
    public NotificationSourceKind SourceKind     { get; private set; }
    public Guid                   SourceId       { get; private set; }
    public DateTimeOffset         CreatedAtUtc   { get; private set; }
    public DateTimeOffset?        DismissedAtUtc { get; private set; }
    public DateTimeOffset?        ReadAtUtc      { get; private set; }
    public DateTimeOffset?        DeletedAtUtc   { get; private set; }
}
