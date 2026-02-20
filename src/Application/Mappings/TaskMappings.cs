using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Domain.Items.Components.Tasks;

namespace Misa.Application.Mappings;

public static class TaskMappings
{
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