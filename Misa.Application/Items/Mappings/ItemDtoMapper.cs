using Misa.Application.Entities.Mappings;
using Misa.Contract.Audit.Lookups;
using Misa.Contract.Items;
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
        => deadline == null ? null : new ScheduleDeadlineDto(deadline.ItemId, deadline.DeadlineAtUtc);
    public static List<ReadItemDto> ToReadItemDto(this List<Item> items)
        => items.Select(i => new ReadItemDto
        {
            Entity = i.Entity.ToReadEntityDto(),
            State = i.State.ToDto(),
            Priority = i.Priority.ToDto(),
            Category = i.Category.ToDto(),
            Title = i.Title
        }).ToList();
    public static StateDto ToDto(this Misa.Domain.Items.State state)
        => new()
        {
            Id = state.Id,
            Name = state.Name,
            Synopsis = state.Synopsis
        };
    public static List<StateDto> ToDto(this List<State> states)
        => states.Select(s => new StateDto
        {
            Id = s.Id,
            Name = s.Name,
            Synopsis = s.Synopsis
        }).ToList();
    public static PriorityDto ToDto(this Misa.Domain.Items.Priority priority)
        => new()
        {
            Id = priority.Id,
            Name = priority.Name,
            Synopsis = priority.Synopsis
        };

    public static List<PriorityDto> ToDto(this List<Priority> priorities)
        => priorities.Select(p => new PriorityDto
        {
            Id = p.Id,
            Name = p.Name,
            Synopsis = p.Synopsis
        }).ToList();
    public static CategoryDto ToDto(this Misa.Domain.Items.Category category)
        => new()
        {
            Id = category.Id,
            Name = category.Name,
            Synopsis = category.Synopsis
        };

    public static List<CategoryDto> ToDto(this List<Category> categories)
        => categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Synopsis = c.Synopsis
        }).ToList();
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