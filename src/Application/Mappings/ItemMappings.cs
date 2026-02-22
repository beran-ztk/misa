using Misa.Contract.Items;
using Misa.Domain.Exceptions;
using Misa.Domain.Items;

namespace Misa.Application.Mappings;
public static class ItemMappings
{
    public static ItemDto ToDto(this Item item)
    {
        return new ItemDto
        {
            Id = item.Id.Value,
            OwnerId = item.OwnerId,
            Workflow = item.Workflow.ToDto(),

            Title = item.Title,
            Description = item.Description,

            IsDeleted = item.IsDeleted,
            IsArchived = item.IsArchived,

            CreatedAt = item.CreatedAt,
            ModifiedAt = item.ModifiedAt
        };
    }

    public static ItemDto TaskToItemDto(this Item item)
    {
        if (item.Workflow != Workflow.Task || item.Activity == null || item.TaskExtension == null)
            throw new DomainValidationException(nameof(item.TaskExtension), "", "Could not map task to dto, because of missing data.");
        
        var temp = item.ToDto();
        temp.Activity = item.Activity.ToDto();
        temp.TaskExtension = item.TaskExtension.ToDto();
        return temp;
    }
    public static ItemDto ScheduleToItemDto(this Item item)
    {
        if (item.Workflow != Workflow.Schedule || item.ScheduleExtension == null)
            throw new DomainValidationException(nameof(item.TaskExtension), "", "Could not map task to dto, because of missing data.");
        
        var temp = item.ToDto();
        temp.ScheduleExtension = item.ScheduleExtension.ToDto();
        return temp;
    }
    public static WorkflowDto ToDto(this Workflow workflow) =>
        workflow switch
        {
            Workflow.Task => WorkflowDto.Task,
            Workflow.Schedule => WorkflowDto.Scheduling,
            _ => throw new ArgumentOutOfRangeException(nameof(workflow), workflow, null)
        };
}