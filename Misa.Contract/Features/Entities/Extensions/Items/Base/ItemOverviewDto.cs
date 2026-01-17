using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Features;

namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public class ItemOverviewDto
{
    public EntityResolvedDto Entity { get; set; }
    public ItemResolvedDto Item { get; set; }
    public ICollection<DescriptionDto> Descriptions { get; set; }
}