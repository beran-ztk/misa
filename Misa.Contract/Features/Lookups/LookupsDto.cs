using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

namespace Misa.Contract.Features.Lookups;

public class LookupsDto
{
    public List<StateDto> States { get; set; }
    public List<SessionEfficiencyTypeDto> EfficiencyTypes { get; set; }
    public List<SessionConcentrationTypeDto> ConcentrationTypes { get; set; }
}