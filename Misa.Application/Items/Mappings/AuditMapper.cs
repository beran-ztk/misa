using Misa.Contract.Audit;
using Misa.Contract.Audit.Lookups;
using Misa.Contract.Audit.Session;
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
    
}