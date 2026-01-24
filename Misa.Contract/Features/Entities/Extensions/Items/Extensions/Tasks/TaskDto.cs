using Misa.Contract.Features.Entities.Extensions.Items.Base;

namespace Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
public class TaskDto
{
    public Guid Id { get; set; }
    public TaskCategoryContract CategoryContract { get; set; }
    
    public ItemDto Item { get; set; }
}