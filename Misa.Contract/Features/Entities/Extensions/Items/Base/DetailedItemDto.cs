using System.Text.Json;
using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;

namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public sealed class DetailedItemDto
{
    public required WorkflowContract Kind { get; init; }
    public required ItemDto Item { get; init; } // Has Entity + Descriptions
    
    public JsonElement? Extension { get; init; }
}
public sealed class TaskDetailsDto
{
    public required TaskCategoryContract Category { get; init; }
}