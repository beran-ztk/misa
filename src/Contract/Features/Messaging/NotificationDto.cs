namespace Misa.Contract.Features.Messaging;

public class NotificationDto
{
    public required NotificationTypeDto NotificationType { get; init; }
    public required NotificationSeverityDto NotificationSeverity { get; init; }
    public string Payload { get; init; } = string.Empty;
    public required DateTimeOffset Timestamp { get; init; }
}