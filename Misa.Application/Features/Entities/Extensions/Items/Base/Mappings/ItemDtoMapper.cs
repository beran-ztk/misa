using Misa.Application.Features.Entities.Base.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Deadlines;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;
using Priority = Misa.Domain.Features.Entities.Extensions.Items.Base.Priority;

namespace Misa.Application.Features.Entities.Extensions.Items.Base.Mappings;

public static class ItemDtoMapper
{
    public static Item ToDomain(this CreateItemDto dto)
    {
        return new Item
            (
                stateId:  dto.StateId,
                priority:  dto.Priority.ToDomain(),
                title:  dto.Title
            );
    }

    public static Priority ToDomain(this Misa.Contract.Features.Entities.Extensions.Items.Base.Priority priority) =>
        (Priority)priority;
    public static ReadItemDto ToReadItemDto(this Item domain)
        => new()
        {
            Id = domain.Id,
            Entity = domain.Entity.ToReadEntityDto(),
            State = domain.State.ToDto(),
            Priority = domain.Priority.ToString(),
            Title = domain.Title,
            ScheduledDeadline = ToDto(domain.ScheduledDeadline),
            HasDeadline = domain.ScheduledDeadline is not null
        };

    public static ScheduleDeadlineDto? ToDto(ScheduledDeadline? deadline)
        => deadline == null ? null : new ScheduleDeadlineDto(deadline.DeadlineAtUtc);
    public static StateDto ToDto(this State s)
        => new(s.Id, s.Name, s.Synopsis);
    public static List<StateDto> ToDto(this List<State> states)
        => states.Select(s => s.ToDto()).ToList();
}