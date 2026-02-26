namespace Misa.Contract.Routes;

public class ActivityRoutes
{
    // Patch
    public const string UpsertDeadline = "items/{itemId:guid}/activities/deadline";
    public static string UpsertDeadlineRequest(Guid itemId) => $"items/{itemId}/activities/deadline";
    
    // Session
    // Session
    public const string StartSessionRoute    = "items/{itemId:guid}/activities/session";
    public const string PauseSessionRoute    = "items/{itemId:guid}/activities/session/pause";
    public const string ContinueSessionRoute = "items/{itemId:guid}/activities/session/continue";
    public const string StopSessionRoute     = "items/{itemId:guid}/activities/session/stop";

    public static string RequestStartSession(Guid itemId)    => $"items/{itemId}/activities/session";
    public static string RequestPauseSession(Guid itemId)    => $"items/{itemId}/activities/session/pause";
    public static string RequestContinueSession(Guid itemId) => $"items/{itemId}/activities/session/continue";
    public static string RequestStopSession(Guid itemId)     => $"items/{itemId}/activities/session/stop";
}