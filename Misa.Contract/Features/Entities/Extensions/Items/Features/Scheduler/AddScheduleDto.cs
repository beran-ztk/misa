namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;

public sealed class AddScheduleDto
{
    public string Title { get; init; } = string.Empty;
    public required ScheduleFrequencyTypeContract ScheduleFrequencyType { get; init; }
    public required int FrequencyInterval { get; init; }
}
