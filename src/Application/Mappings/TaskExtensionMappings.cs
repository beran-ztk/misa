using Misa.Contract.Items.Components.Tasks;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Tasks;

namespace Misa.Application.Mappings;

public static class TaskExtensionMappings
{
    public static TaskExtensionDto ToTaskExtensionDto(this Item item)
    {
        if (item.Activity is null || item.TaskExtension is null)
            throw new ArgumentNullException(nameof(item.Activity));
        
        return new TaskExtensionDto
        {
            Item = item.ToDto(),
            Activity = item.Activity.ToDto(),
            Category = item.TaskExtension.Category.ToDto()
        };
    }

    public static IReadOnlyCollection<TaskExtensionDto> ToTaskExtensionDto(this IEnumerable<Item> items)
        => items.Select(i => i.ToTaskExtensionDto()).ToList();
    public static TaskCategory ToDomain(this TaskCategoryDto categoryDto) =>
        categoryDto switch
        {
            TaskCategoryDto.Personal => TaskCategory.Personal,
            TaskCategoryDto.School => TaskCategory.School,
            TaskCategoryDto.Work => TaskCategory.Work,
            TaskCategoryDto.Other => TaskCategory.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(categoryDto), categoryDto, null)
        };
    public static TaskCategoryDto ToDto(this TaskCategory domainCategory) =>
        domainCategory switch
        {
            TaskCategory.Personal => TaskCategoryDto.Personal,
            TaskCategory.School   => TaskCategoryDto.School,
            TaskCategory.Work     => TaskCategoryDto.Work,
            TaskCategory.Other    => TaskCategoryDto.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(domainCategory), domainCategory, null)
        };
}