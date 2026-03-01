using Misa.Contract.Items;
using Misa.Contract.Items.Components.Schola;
using Misa.Contract.Items.Components.Tasks;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schola;

namespace Misa.Application.Mappings;

public static class ScholaMappings
{
    private static ArcExtensionDto ToDto(this Arc arc)
    {
        return new ArcExtensionDto
        {
            Id = arc.Id.Value
        };
    }
    private static UnitExtensionDto ToDto(this Unit unit)
    {
        return new UnitExtensionDto
        {
            Id = unit.Id.Value,
            ArcId = unit.ArcId?.Value
        };
    }
    public static ItemDto ToArcExtensionDto(this Item item)
    {
        if (item.Activity is null || item.Arc is null)
            throw new ArgumentNullException(nameof(item.Arc));

        var itemDto = item.ToDto();
        itemDto.Activity = item.Activity.ToDto();
        itemDto.ArcExtension = item.Arc.ToDto();

        return itemDto;
    }
    public static ItemDto ToUnitExtensionDto(this Item item)
    {
        if (item.Activity is null || item.Unit is null)
            throw new ArgumentNullException(nameof(item.Unit));

        var itemDto = item.ToDto();
        itemDto.Activity = item.Activity.ToDto();
        itemDto.UnitExtension = item.Unit.ToDto();

        return itemDto;
    }
}