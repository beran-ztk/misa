using System.Text.Json;
using Misa.Contract.Features.Common.Deadlines;
using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;

namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public sealed class DetailedItemDto
{
    public required WorkflowDto Kind { get; init; }
    public required ItemDto Item { get; init; }
    public DeadlineDto? Deadline { get; init; }
    public JsonElement? Extension { get; init; }
}
public sealed class TaskDetailsDto
{
    public required TaskCategoryDto Category { get; init; }
}