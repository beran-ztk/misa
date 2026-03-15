namespace Misa.Contract.Routes;

public static class NotificationRoutes
{
    public const string GetAll      = "notifications";
    public const string Dismiss     = "notifications/{id}";
    public const string MarkRead    = "notifications/{id}/read";
    public const string MarkAllRead = "notifications/read-all";
}
