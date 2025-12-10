using Misa.Application.Entities.Mappings;
using Misa.Contract.Items;
using Misa.Contract.Items.Lookups;
using Misa.Domain.Entities;
using Misa.Domain.Items;

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
        => new ReadItemDto
        {
            Entity = domain.Entity.ToReadEntityDto(),
            State = domain.State.ToDto(),
            Priority = domain.Priority.ToDto(),
            Category = domain.Category.ToDto(),
            Title = domain.Title
        };
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
}