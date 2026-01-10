using Misa.Contract.Audit;
using Misa.Contract.Descriptions;
using Misa.Contract.Entities;
using Misa.Contract.Items;
using Misa.Contract.Items.Common;

namespace Misa.Contract.Main;

public class DetailDto
{
    public required ReadEntityDto Entity { get; set; }
    public required ReadItemDto Item { get; set; }
    
    public List<DescriptionResolvedDto>? Descriptions { get; set; }
    public List<ReadRelationDto>? Relations { get; set; }
    public List<SessionDto>? Sessions { get; set; }
    public List<ActionDto>? Actions { get; set; }
}