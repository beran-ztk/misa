using Misa.Contract.Items.Components.Activity;

namespace Misa.Contract.Items.Components.Tasks;

public record UpdateTaskRequest(
    string? Title,
    string? Description,
    ActivityStateDto? ActivityState,
    ActivityPriorityDto? ActivityPriority,
    TaskCategoryDto? TaskCategory,
    string? Reason = null);
