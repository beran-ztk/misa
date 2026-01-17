using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Deadlines;

namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public class ReadItemDto
{
    public Guid EntityId { get; set; }
    public ReadEntityDto Entity { get; set; } = null!;
    

    public StateDto State { get; set; } = null!;
    public PriorityDto Priority { get; set; } = null!;
    public CategoryDto Category { get; set; } = null!;
    public string Title { get; set; } = null!;
    public ScheduleDeadlineDto? ScheduledDeadline { get; set; }
    public bool HasDeadline { get; set; }

}