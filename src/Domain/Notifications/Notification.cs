namespace Misa.Domain.Notifications;

public sealed class Notification
{
    private Notification() { } // EF Core

    public Notification(
        Guid id,
        string title,
        string message,
        NotificationSourceKind sourceKind,
        Guid sourceId,
        DateTimeOffset createdAtUtc,
        string ownerId)
    {
        Id           = id;
        Title        = title;
        Message      = message;
        SourceKind   = sourceKind;
        SourceId     = sourceId;
        CreatedAtUtc = createdAtUtc;
        OwnerId      = ownerId;
    }

    public Guid                   Id             { get; private set; }
    public string                 Title          { get; private set; } = string.Empty;
    public string                 Message        { get; private set; } = string.Empty;
    public NotificationSourceKind SourceKind     { get; private set; }
    public Guid                   SourceId       { get; private set; }
    public DateTimeOffset         CreatedAtUtc   { get; private set; }
    public string                 OwnerId        { get; private set; } = string.Empty;
    public DateTimeOffset?        DismissedAtUtc { get; private set; }
    public DateTimeOffset?        ReadAtUtc      { get; private set; }
}
