using Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Commands;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks;
using Task = Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task;

namespace Misa.Application.Mappings;

public static class TaskMappings
{
    public static List<TaskDto> ToDto(this List<Task> tasks)
    {
        return tasks.Select(t => t.ToDto()).ToList();
    }
    public static TaskDto ToDto(this Task task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Category = task.Category.MapToDto(),
            Item = task.Item.ToDto()
        };
    }
    public static AddTaskCommand ToCommand(this AddTaskDto dto)
        => new(
            dto.Title,
            dto.CategoryContract,
            dto.PriorityDto,
            dto.Deadline
        );
    public static TaskCategory MapToDomain(this TaskCategoryContract categoryContract) =>
        categoryContract switch
        {
            TaskCategoryContract.Personal => TaskCategory.Personal,
            TaskCategoryContract.School => TaskCategory.School,
            TaskCategoryContract.Work => TaskCategory.Work,
            TaskCategoryContract.Other => TaskCategory.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(categoryContract), categoryContract, null)
        };
    public static TaskCategoryContract MapToDto(this TaskCategory domainCategory) =>
        domainCategory switch
        {
            TaskCategory.Personal => TaskCategoryContract.Personal,
            TaskCategory.School   => TaskCategoryContract.School,
            TaskCategory.Work     => TaskCategoryContract.Work,
            TaskCategory.Other    => TaskCategoryContract.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(domainCategory), domainCategory, null)
        };
}