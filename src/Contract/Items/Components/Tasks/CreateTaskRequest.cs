using Misa.Contract.Items.Components.Activity;

namespace Misa.Contract.Items.Components.Tasks;

public sealed record CreateTaskRequest(
    string Title,
    string Description,
    TaskCategoryDto CategoryDto,
    ActivityPriorityDto ActivityPriorityDto,
    DateTimeOffset? DueDate
);