namespace Misa.Contract.Routes;

public class ScheduleRoutes
{
    // Create
    public const string CreateSchedule = "items/schedule";
    
    // Get
    public const string GetSchedules = "items/schedules";

    // Put
    public const string UpdateSchedule = "items/{itemId:guid}/schedule";
    public static string UpdateScheduleRequest(Guid itemId) => $"items/{itemId}/schedule";
}
