using Misa.Contract.Features.Entities.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Application.Mappings;
public static class ItemMappings
{
    public static ItemDto ToDto(this Item item)
    {
        return new ItemDto
        {
            Id = item.Id,
            State = (ItemStateDto)item.State,
            Priority = item.Priority.MapToDto(),
            Title = item.Title,
            Entity = item.Entity.ToDto()
        };
    }
    public static Priority MapToDomain(this PriorityDto priorityDto) =>
        priorityDto switch
        {
            PriorityDto.None => Priority.None,
            PriorityDto.Low => Priority.Low,
            PriorityDto.Medium => Priority.Medium,
            PriorityDto.High => Priority.High,
            PriorityDto.Urgent => Priority.Urgent,
            PriorityDto.Critical => Priority.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(priorityDto), priorityDto, null)
        };
    public static PriorityDto MapToDto(this Priority priority) =>
        priority switch
        {
            Priority.None     => PriorityDto.None,
            Priority.Low      => PriorityDto.Low,
            Priority.Medium   => PriorityDto.Medium,
            Priority.High     => PriorityDto.High,
            Priority.Urgent   => PriorityDto.Urgent,
            Priority.Critical => PriorityDto.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
        };
    public static WorkflowDto MapToDto(this Workflow workflow) =>
        workflow switch
        {
            Workflow.Task => WorkflowDto.Task,
            Workflow.Deadline => WorkflowDto.Deadline,
            Workflow.Scheduling => WorkflowDto.Scheduling,
            _ => throw new ArgumentOutOfRangeException(nameof(workflow), workflow, null)
        };
}