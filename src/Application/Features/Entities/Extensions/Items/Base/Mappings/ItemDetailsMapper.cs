using Misa.Contract.Features.Entities.Features;
using Misa.Domain.Features.Entities.Features;

namespace Misa.Application.Features.Entities.Extensions.Items.Base.Mappings;

public static class ItemDetailsMapper
{
    public static DescriptionDto ToDto(this Description domain)
        => new(
            domain.Id,
            domain.EntityId,
            domain.Content,
            domain.CreatedAtUtc
        );
    public static ICollection<DescriptionDto> ToDto(this ICollection<Description> domains)
        => domains
            .Select(d => d.ToDto())
            .ToList();
}