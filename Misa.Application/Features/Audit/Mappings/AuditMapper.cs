using Misa.Contract.Features.Actions;
using Misa.Domain.Features.Audit;

namespace Misa.Application.Features.Audit.Mappings;

public static class AuditMapper
{
    public static List<ActionDto> ToDto(this ICollection<AuditChange> a)
        => a.Select(x => new ActionDto()
        {
            Id = x.Id,
            EntityId = x.EntityId,
            ValueBefore = x.ValueBefore,
            ValueAfter = x.ValueAfter,
            Reason = x.Reason,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();
    
}