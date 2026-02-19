using Misa.Application.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Domain.Items.Components.Activities.Sessions;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Mappings;

public static class SessionMapper
{
    public static SessionResolvedDto ToDto(this Session x) 
        => new()
        {
            Id = x.Id,
            EntityId = x.ItemId,
        
            State = x.State.ToDto(),
            Efficiency = x.Efficiency.MapToDto(),
            Concentration = x.Concentration.MapToDto(),
        
            Objective = x.Objective,
            Summary = x.Summary,
            AutoStopReason = x.AutoStopReason,
        
            PlannedDuration = x.PlannedDuration,
        
            StopAutomatically = x.StopAutomatically,
            WasAutomaticallyStopped = x.WasAutomaticallyStopped,
            CreatedAtUtc = x.CreatedAtUtc,
            Segments = x.Segments.ToDto()
        };
    private static SessionStateDto ToDto(this SessionState state)
        => (SessionStateDto)state;
    private static SessionSegmentDto ToDto(this SessionSegment x) =>
        new(
            x.Id,
            x.SessionId,
            x.PauseReason,
            x.StartedAtUtc,
            x.EndedAtUtc
        );

    private static List<SessionSegmentDto> ToDto(this ICollection<SessionSegment> segments) =>
        segments.Select(x => x.ToDto()).ToList();
}