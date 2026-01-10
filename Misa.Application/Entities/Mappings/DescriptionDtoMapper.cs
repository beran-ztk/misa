using Misa.Contract.Descriptions;
using Misa.Domain.Extensions;

namespace Misa.Application.Entities.Mappings;

public static class DescriptionDtoMapper
{
    public static List<DescriptionResolvedDto> ToDto(this ICollection<Description> dto)
        => dto.Select(x => x.ToDto()).ToList();
    private static DescriptionResolvedDto ToDto(this Description dto)
        => new()
        {
            Id = dto.Id,
            EntityId = dto.EntityId,
            Type = dto.Type.ToDto(),
            Content = dto.Content,
            CreatedAtUtc = dto.CreatedAtUtc
        };
    private static DescriptionTypeDto ToDto(this DescriptionTypes x) => new(x.Id, x.Name, x.Synopsis);
}