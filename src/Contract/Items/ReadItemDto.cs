using Misa.Contract.Features.Entities.Base;

namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public class ReadItemDto
{
    public Guid Id { get; set; }
    public ReadEntityDto Entity { get; set; } = null!;
    
    public ItemStateDto State { get; set; }
    public string Priority { get; set; } = null!;
    public string Title { get; set; } = null!;
    public bool HasDeadline { get; set; }
}