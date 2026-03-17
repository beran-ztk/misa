namespace Misa.Contract.Notifications;

public record NotificationLinkTarget(
    NotificationWorkspaceTarget Workspace,
    Guid                        ItemId);
