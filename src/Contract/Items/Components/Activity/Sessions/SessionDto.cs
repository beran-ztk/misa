namespace Misa.Contract.Items.Components.Activity.Sessions;

public sealed record SessionDto
{
    public required Guid Id { get; init; }
    public required Guid ItemId { get; init; }
    public required SessionStateDto State { get; init; }
    public required SessionEfficiencyDto Efficiency { get; init; }
    public required SessionConcentrationDto Concentration { get; init; }

    public string? Objective { get; init; }
    public string? Summary { get; init; }
    public string? AutoStopReason { get; init; }

    public TimeSpan? PlannedDuration { get; init; }

    public required bool StopAutomatically { get; init; }
    public bool? WasAutomaticallyStopped { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required ICollection<SessionSegmentDto> Segments { get; init; }
}