using Misa.Contract.Entities.Features;
using Misa.Domain.Entities.Extensions;

namespace Misa.Application.Entities.Mappings;

public static class DescriptionMapper
{
    public static List<DescriptionDto> ToDto(this ICollection<Description> dto)
        => dto.Select(x => x.ToDto()).ToList();

    private static DescriptionDto ToDto(this Description dto)
        => new(dto.Id, dto.EntityId, dto.Content, dto.CreatedAtUtc);
}