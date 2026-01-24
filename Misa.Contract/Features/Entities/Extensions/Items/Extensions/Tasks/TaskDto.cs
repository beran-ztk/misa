using Misa.Contract.Features.Entities.Extensions.Items.Base;

namespace Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
public class TaskDto
{
    public required Guid Id { get; init; }
    public required TaskCategoryContract Category { get; init; }
    
    public required ItemDto Item { get; init; }
}