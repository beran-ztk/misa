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
        Efficiency = x.Efficiency?.ToDto(),
        Concentration = x.Concentration?.ToDto(),
        Objective = x.Objective,
        Summary = x.Summary,
        AutoStopReason = x.AutoStopReason,
        PlannedDuration = x.PlannedDuration,
        ActualDuration = x.ActualDuration,
        StopAutomatically = x.StopAutomatically,
        StartedAtUtc = x.StartedAtUtc,
        EndedAtUtc = x.EndedAtUtc
    }).ToList();
}