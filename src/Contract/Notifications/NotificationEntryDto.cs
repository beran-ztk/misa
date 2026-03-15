namespace Misa.Contract.Notifications;

public record NotificationEntryDto(
    Guid             Id,
    string           Title,
    string           Message,
    Guid             SourceId,
    DateTimeOffset   CreatedAtUtc,
    DateTimeOffset?  ReadAtUtc);
