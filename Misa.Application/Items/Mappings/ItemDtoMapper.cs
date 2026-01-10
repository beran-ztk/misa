using Misa.Application.Entities.Mappings;
using Misa.Contract.Audit.Lookups;
using Misa.Contract.Items;
using Misa.Contract.Items.Common;
using Misa.Contract.Items.Lookups;
using Misa.Contract.Scheduling;
using Misa.Domain.Audit;
using Misa.Domain.Entities;
using Misa.Domain.Items;
using Misa.Domain.Scheduling;

namespace Misa.Application.Items.Mappings;

public static class ItemDtoMapper
{
    public static Item ToDomain(this CreateItemDto dto, Entity entity)
    {
        return new Item
            (
                entity: entity,
                
                stateId:  dto.StateId,
                priorityId:  dto.PriorityId,
                categoryId:  dto.CategoryId,
                title:  dto.Title
            );
    }

    public static ReadItemDto ToReadItemDto(this Item domain)
        => new()
        {
            EntityId = domain.EntityId,
            Entity = domain.Entity.ToReadEntityDto(),
            State = domain.State.ToDto(),
            Priority = domain.Priority.ToDto(),
            Category = domain.Category.ToDto(),
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
    public static PriorityDto ToDto(this Misa.Domain.Items.Priority s)
        => new(s.Id, s.Name, s.Synopsis);

    public static List<PriorityDto> ToDto(this List<Priority> priorities)
        => priorities.Select(p => p.ToDto()).ToList();
    public static CategoryDto ToDto(this Misa.Domain.Items.Category s)
        => new(s.Id, s.Name, s.Synopsis);

    public static List<CategoryDto> ToDto(this List<Category> categories)
        => categories.Select(c => c.ToDto()).ToList();
    
    public static List<SessionEfficiencyTypeDto> ToDto(this List<SessionEfficiencyType> categories)
        => categories.Select(c => new SessionEfficiencyTypeDto
        {
            Id = c.Id,
            Name = c.Name,
            Synopsis = c.Synopsis
        }).ToList();
    public static List<SessionConcentrationTypeDto> ToDto(this List<SessionConcentrationType> categories)
        => categories.Select(c => new SessionConcentrationTypeDto
        {
            Id = c.Id,
            Name = c.Name,
            Synopsis = c.Synopsis
        }).ToList();
}