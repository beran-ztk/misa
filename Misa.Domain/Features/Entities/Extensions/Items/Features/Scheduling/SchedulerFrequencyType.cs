namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

/// <summary>
/// Represents a frequency type for scheduled tasks.
/// </summary>
public sealed class SchedulerFrequencyType
{
    private SchedulerFrequencyType() { } // EF Core

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Synopsis { get; private set; }
}
