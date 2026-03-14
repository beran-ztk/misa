namespace Misa.Contract.Items.Components.Schedules;

public record UpdateScheduleRequest(
    string? Title,
    string? Description,
    ScheduleMisfirePolicyDto? MisfirePolicy);
