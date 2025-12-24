using Misa.Contract.Audit;
using Misa.Contract.Audit.Lookups;
using Misa.Domain.Audit;

namespace Misa.Application.Items.Mappings;

public static class AuditMapper
{
    public static ActionTypeDto ToDto(this ActionType x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Synopsis = x.Synopsis
    };

    public static SessionEfficiencyTypeDto ToDto(this SessionEfficiencyType x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Synopsis = x.Synopsis,
        SortOrder = x.SortOrder
    };

    public static SessionConcentrationTypeDto ToDto(this SessionConcentrationType x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Synopsis = x.Synopsis,
        SortOrder = x.SortOrder
    };
    
    public static SessionStateDto ToDto(this SessionStates x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Synopsis = x.Synopsis
    };

    public static List<ActionDto> ToDto(this ICollection<Misa.Domain.Audit.Action> a)
        => a.Select(x => new ActionDto()
        {
            Id = x.Id,
            EntityId = x.EntityId,
            Type = x.Type.ToDto(),
            ValueBefore = x.ValueBefore,
            ValueAfter = x.ValueAfter,
            Reason = x.Reason,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();

    public static List<SessionDto> ToDto(this ICollection<Session> s) 
        => s.Select(x => new SessionDto()
    {
        Id = x.Id,
        EntityId = x.EntityId,
        
        State = x.State?.ToDto(),
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
    }).ToList();
    
    public static List<SessionSegmentDto> ToDto(this ICollection<SessionSegment> s) 
        => s.Select(x => new SessionSegmentDto()
        {
            Id = x.Id,
            SessionId = x.SessionId,
            PauseReason = x.PauseReason,
        
            StartedAtUtc = x.StartedAtUtc,
            EndedAtUtc = x.EndedAtUtc
        }).ToList();
}