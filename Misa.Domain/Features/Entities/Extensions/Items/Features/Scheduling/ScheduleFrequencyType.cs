namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

/// <summary>
/// Defines the base frequency type for a scheduler.
/// </summary>
public enum ScheduleFrequencyType
{
    Once,
    Minutes,
    Hours,
    Days,
    Weeks,
    Months,
    Years
}