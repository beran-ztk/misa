using Misa.Contract.Items.Components.Schola;
using Misa.Domain.Items;

namespace Misa.Application.Mappings;

public static class ScholaMappings
{
    public static ArcDto ToArcDto(this Item item)
    {
        if (item.Activity is null || item.Arc is null)
            throw new ArgumentNullException(nameof(item.Arc));

        return new ArcDto
        {
            Item = item.ToDto(),
            Activity = item.Activity.ToDto()
        };
    }
    public static UnitDto ToUnitDto(this Item item)
    {
        if (item.Activity is null || item.Unit is null)
            throw new ArgumentNullException(nameof(item.Unit));

        return new UnitDto
        {
            Item = item.ToDto(),
            Activity = item.Activity.ToDto(),
            
            ArcId = item.Unit.ArcId?.Value
        };
    }
}