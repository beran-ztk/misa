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

    public TimeSpan TotalElapsed => TimeSpan.FromTicks(
        Segments
            .Where(s => s.EndedAtUtc is not null)
            .Sum(s => (s.EndedAtUtc!.Value - s.StartedAtUtc).Ticks));

    public string DurationDisplay
    {
        get
        {
            var elapsed = TotalElapsed;
            if (elapsed == TimeSpan.Zero)
                return PlannedDuration.HasValue
                    ? $"0 / {(int)PlannedDuration.Value.TotalMinutes}m"
                    : "0m";

            var elapsedStr = elapsed.TotalHours >= 1
                ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m"
                : $"{(int)elapsed.TotalMinutes}m";

            if (PlannedDuration is null)
                return elapsedStr;

            var plannedStr = PlannedDuration.Value.TotalHours >= 1
                ? $"{(int)PlannedDuration.Value.TotalHours}h {PlannedDuration.Value.Minutes}m"
                : $"{(int)PlannedDuration.Value.TotalMinutes}m";

            return $"{elapsedStr} / {plannedStr}";
        }
    }
}