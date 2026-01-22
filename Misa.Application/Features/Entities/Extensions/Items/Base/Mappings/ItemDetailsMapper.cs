using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Features;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Features.Descriptions;

namespace Misa.Application.Features.Entities.Extensions.Items.Base.Mappings;

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
            domain.Id,
            domain.State.ToDto(),
            domain.Priority.ToString(),
            domain.Title
        );
    }
    public static EntityResolvedDto ToResolvedDto(this Entity domain)
    {
        return new EntityResolvedDto(
            domain.Id,

            domain.Workflow.ToString(),
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
    public static ICollection<DescriptionDto> ToDto(this ICollection<Description> domains)
        => domains
            .Select(d => d.ToDto())
            .ToList();
}