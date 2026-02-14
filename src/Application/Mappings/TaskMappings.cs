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
    public static TaskCategory MapToDomain(this TaskCategoryDto categoryDto) =>
        categoryDto switch
        {
            TaskCategoryDto.Personal => TaskCategory.Personal,
            TaskCategoryDto.School => TaskCategory.School,
            TaskCategoryDto.Work => TaskCategory.Work,
            TaskCategoryDto.Other => TaskCategory.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(categoryDto), categoryDto, null)
        };
    public static TaskCategoryDto MapToDto(this TaskCategory domainCategory) =>
        domainCategory switch
        {
            TaskCategory.Personal => TaskCategoryDto.Personal,
            TaskCategory.School   => TaskCategoryDto.School,
            TaskCategory.Work     => TaskCategoryDto.Work,
            TaskCategory.Other    => TaskCategoryDto.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(domainCategory), domainCategory, null)
        };
}