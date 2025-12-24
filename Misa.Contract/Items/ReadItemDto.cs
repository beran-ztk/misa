using Misa.Contract.Entities;
using Misa.Contract.Items.Lookups;

namespace Misa.Contract.Items;

public class ReadItemDto
{
    public ReadEntityDto Entity { get; set; } = null!;
    

    public StateDto State { get; set; } = null!;
    public PriorityDto Priority { get; set; } = null!;
    public CategoryDto Category { get; set; } = null!;
    public string Title { get; set; } = null!;
}