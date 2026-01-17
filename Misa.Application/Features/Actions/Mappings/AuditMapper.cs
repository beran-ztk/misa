using Misa.Contract.Features.Actions;
using Misa.Domain.Features.Actions;
using Action = Misa.Domain.Features.Actions.Action;

namespace Misa.Application.Features.Actions.Mappings;

public static class AuditMapper
{
    public static ActionTypeDto ToDto(this ActionType x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Synopsis = x.Synopsis
    };
    public static List<ActionDto> ToDto(this ICollection<Action> a)
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
    
}