using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Domain.Items.Components.Activities.Sessions;

namespace Misa.Application.Mappings;

public static class SessionMappings
{
    public static SessionDto ToDto(this Session session)
    {
        return new SessionDto
        {
            Id = session.Id,
            ItemId = session.ItemId.Value,
            State = session.State.ToDto(),
            Efficiency = session.Efficiency.ToDto(),
            Concentration = session.Concentration.ToDto(),

            Objective = session.Objective,
            Summary = session.Summary,
            AutoStopReason = session.AutoStopReason,

            PlannedDuration = session.PlannedDuration,

            StopAutomatically = session.StopAutomatically,
            WasAutomaticallyStopped = session.WasAutomaticallyStopped,

            CreatedAtUtc = session.CreatedAtUtc,

            Segments = session.Segments.Select(x => x.ToDto()).ToList()
        };
    }
    public static SessionSegmentDto ToDto(this SessionSegment segment)
        => new(
            Id: segment.Id,
            SessionId: segment.SessionId,
            StartedAtUtc: segment.StartedAtUtc,
            EndedAtUtc: segment.EndedAtUtc,
            PauseReason: segment.PauseReason
        );
    public static SessionState ToDomain(this SessionStateDto dto) =>
        dto switch
        {
            SessionStateDto.Running => SessionState.Running,
            SessionStateDto.Paused  => SessionState.Paused,
            SessionStateDto.Ended   => SessionState.Ended,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, null)
        };

    public static SessionStateDto ToDto(this SessionState domain) =>
        domain switch
        {
            SessionState.Running => SessionStateDto.Running,
            SessionState.Paused  => SessionStateDto.Paused,
            SessionState.Ended   => SessionStateDto.Ended,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };
    public static SessionEfficiencyType ToDomain(this SessionEfficiencyDto dto) =>
        dto switch
        {
            SessionEfficiencyDto.None            => SessionEfficiencyType.None,
            SessionEfficiencyDto.LowOutput       => SessionEfficiencyType.LowOutput,
            SessionEfficiencyDto.SteadyOutput    => SessionEfficiencyType.SteadyOutput,
            SessionEfficiencyDto.HighOutput      => SessionEfficiencyType.HighOutput,
            SessionEfficiencyDto.PeakPerformance => SessionEfficiencyType.PeakPerformance,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, null)
        };

    public static SessionEfficiencyDto ToDto(this SessionEfficiencyType domain) =>
        domain switch
        {
            SessionEfficiencyType.None            => SessionEfficiencyDto.None,
            SessionEfficiencyType.LowOutput       => SessionEfficiencyDto.LowOutput,
            SessionEfficiencyType.SteadyOutput    => SessionEfficiencyDto.SteadyOutput,
            SessionEfficiencyType.HighOutput      => SessionEfficiencyDto.HighOutput,
            SessionEfficiencyType.PeakPerformance => SessionEfficiencyDto.PeakPerformance,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };

    public static SessionConcentrationType ToDomain(this SessionConcentrationDto dto) =>
        dto switch
        {
            SessionConcentrationDto.None              => SessionConcentrationType.None,
            SessionConcentrationDto.Distracted        => SessionConcentrationType.Distracted,
            SessionConcentrationDto.UnfocusedButCalm  => SessionConcentrationType.UnfocusedButCalm,
            SessionConcentrationDto.Focused           => SessionConcentrationType.Focused,
            SessionConcentrationDto.DeepFocus         => SessionConcentrationType.DeepFocus,
            SessionConcentrationDto.Hyperfocus        => SessionConcentrationType.Hyperfocus,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, null)
        };

    public static SessionConcentrationDto ToDto(this SessionConcentrationType domain) =>
        domain switch
        {
            SessionConcentrationType.None              => SessionConcentrationDto.None,
            SessionConcentrationType.Distracted        => SessionConcentrationDto.Distracted,
            SessionConcentrationType.UnfocusedButCalm  => SessionConcentrationDto.UnfocusedButCalm,
            SessionConcentrationType.Focused           => SessionConcentrationDto.Focused,
            SessionConcentrationType.DeepFocus         => SessionConcentrationDto.DeepFocus,
            SessionConcentrationType.Hyperfocus        => SessionConcentrationDto.Hyperfocus,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };
}
