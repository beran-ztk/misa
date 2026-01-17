using Misa.Contract.Features.Entities.Features;
using Misa.Domain.Features.Entities.Features.Descriptions;

namespace Misa.Application.Features.Entities.Features.Descriptions.Mappings;

public static class DescriptionMapper
{
    public static List<DescriptionDto> ToDto(this ICollection<Description> dto)
        => dto.Select(x => x.ToDto()).ToList();

    private static DescriptionDto ToDto(this Description dto)
        => new(dto.Id, dto.EntityId, dto.Content, dto.CreatedAtUtc);
}