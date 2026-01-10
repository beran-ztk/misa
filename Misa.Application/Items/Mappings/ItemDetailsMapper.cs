using Misa.Application.Entities.Mappings;
using Misa.Contract.Entities;
using Misa.Contract.Entities.Features;
using Misa.Contract.Items.Common;
using Misa.Contract.Items.Details;
using Misa.Contract.Items.Lookups;
using Misa.Domain.Entities;
using Misa.Domain.Entities.Extensions;
using Misa.Domain.Items;

namespace Misa.Application.Items.Mappings;

public static class ItemDetailsMapper
{
    public static ItemOverviewDto ToOverviewDto(this Item domain)
    {
        return new ItemOverviewDto
        {
            Entity = domain.Entity.ToResolvedDto(),
            Item = domain.ToResolvedDto(),
            Descriptions = domain.Entity.Descriptions.ToDto()
        };
    }

    public static ItemResolvedDto ToResolvedDto(this Item domain)
    {
        return new ItemResolvedDto(
            domain.EntityId,
            domain.State.ToDto(),
            domain.Priority.ToDto(),
            domain.Category.ToDto(),
            domain.Title
        );
    }
    public static EntityResolvedDto ToResolvedDto(this Entity domain)
    {
        return new EntityResolvedDto(
            domain.Id,
            domain.OwnerId,

            domain.Workflow.ToDto(),
            domain.IsDeleted,
            domain.IsArchived,

            domain.CreatedAt,
            domain.UpdatedAt,
            domain.DeletedAt,
            domain.ArchivedAt,
            domain.InteractedAt
        );
    }
    public static DescriptionDto ToDto(this Description domain)
        => new(
            domain.Id,
            domain.EntityId,
            domain.Content,
            domain.CreatedAtUtc
        );
    public static IReadOnlyList<DescriptionDto> ToDto(this ICollection<Description> domains)
        => domains
            .Select(d => d.ToDto())
            .ToList();
}