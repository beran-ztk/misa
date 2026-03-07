using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Schola;

namespace Misa.Application.Features.Items.Schola;

public record GetArcWithUnitsCommand;

public sealed class GetArcWithUnitsHandler(IItemRepository repository)
{
    public async Task<ScholaDto> HandleAsync(GetArcWithUnitsCommand command)
    {
        var arcs = await repository.GetArcsAsync();
        var units = await repository.GetUnitsAsync();

        var scholaDto = new ScholaDto
        {
            Arcs = arcs.Select(a => a.ToArcDto()).ToList(),
            Units = units.Select(u => u.ToUnitDto()).ToList()
        };

        return scholaDto;
    }
}