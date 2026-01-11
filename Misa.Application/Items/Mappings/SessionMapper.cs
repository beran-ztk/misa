using Misa.Contract.Audit;
using Misa.Contract.Audit.Lookups;
using Misa.Contract.Audit.Session;
using Misa.Domain.Audit;

namespace Misa.Application.Items.Mappings;

public static class SessionMapper
{
    public static SessionEfficiencyTypeDto ToDto(this SessionEfficiencyType x) =>
        new(
            x.Id,
            x.Name,
            x.Synopsis,
            x.SortOrder
        );

    public static SessionConcentrationTypeDto ToDto(this SessionConcentrationType x) =>
        new(
            x.Id,
            x.Name,
            x.Synopsis,
            x.SortOrder
        );

    public static SessionStateDto ToDto(this SessionStates x) =>
        new(
            x.Id,
            x.Name,
            x.Synopsis
        );
    public static List<SessionEfficiencyTypeDto> ToDto(this List<SessionEfficiencyType> ef)
        => ef.Select(c => c.ToDto()).ToList();
    public static List<SessionConcentrationTypeDto> ToDto(this List<SessionConcentrationType> co)
        => co.Select(c => c.ToDto()).ToList();
    public static SessionResolvedDto ToDto(this Session x) 
        => new SessionResolvedDto()
        {
            Id = x.Id,
            EntityId = x.ItemId,
        
            State = x.State.ToDto(),
            Efficiency = x.Efficiency?.ToDto(),
            Concentration = x.Concentration?.ToDto(),
        
            Objective = x.Objective,
            Summary = x.Summary,
            AutoStopReason = x.AutoStopReason,
        
            PlannedDuration = x.PlannedDuration,
        
            StopAutomatically = x.StopAutomatically,
            WasAutomaticallyStopped = x.WasAutomaticallyStopped,
            CreatedAtUtc = x.CreatedAtUtc,
            Segments = x.Segments.ToDto()
        };
    
    public static SessionSegmentDto ToDto(this SessionSegment x) =>
        new(
            x.Id,
            x.SessionId,
            x.PauseReason,
            x.StartedAtUtc,
            x.EndedAtUtc
        );
    public static List<SessionSegmentDto> ToDto(this ICollection<SessionSegment> segments) =>
        segments.Select(x => x.ToDto()).ToList();
}