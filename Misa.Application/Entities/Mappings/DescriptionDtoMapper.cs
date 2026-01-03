using Misa.Contract.Main;
using Misa.Domain.Extensions;

namespace Misa.Application.Entities.Mappings;

public static class DescriptionDtoMapper
{
    public static DescriptionDto ToDto(this Description dto)
        => new DescriptionDto
        {
            Id = dto.Id,
            EntityId = dto.EntityId,
            TypeId = dto.TypeId,
            Content = dto.Content,
            CreatedAtUtc = dto.CreatedAtUtc
        };
    public static List<DescriptionDto> ToDto(this ICollection<Description> dto)
        => dto.Select(x => new DescriptionDto
        {
            Id = x.Id,
            EntityId = x.EntityId,
            TypeId = x.TypeId,
            Content = x.Content,
            CreatedAtUtc = x.CreatedAtUtc,
            Type = x.Type.ToDto()
        }).ToList();

    public static DescriptionTypeDto ToDto(this DescriptionTypes x)
        => new DescriptionTypeDto
        {
            Id = x.Id,
            Name = x.Name,
            Synopsis = x.Synopsis,
            SortOrder = x.SortOrder
        };
}