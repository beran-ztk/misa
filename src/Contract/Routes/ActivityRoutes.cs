namespace Misa.Contract.Routes;

public class ActivityRoutes
{
    // Patch
    public const string UpsertDeadline = "items/{itemId:guid}/activities/deadline";
    public static string UpsertDeadlineRequest(Guid itemId) => $"items/{itemId}/activities/deadline";
}